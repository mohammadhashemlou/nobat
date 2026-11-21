using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Barbers.DTOs;

namespace Nobat.Application.Modules.Barbers.Queries;

public record GetBarbersQuery(Guid SalonId, bool IncludeInactive = false) : IRequest<List<BarberDto>>;

public class GetBarbersHandler : IRequestHandler<GetBarbersQuery, List<BarberDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBarbersHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BarberDto>> Handle(GetBarbersQuery request, CancellationToken ct)
    {
        var query = _db.Barbers.AsNoTracking().Where(b => b.SalonId == request.SalonId);

        if (!request.IncludeInactive)
            query = query.Where(b => b.IsActive);

        var barbers = await query
            .OrderBy(b => b.FullName)
            .Select(b => new BarberDto
            {
                Id = b.Id,
                UserId = b.UserId,
                SalonId = b.SalonId,
                FullName = b.FullName,
                Bio = b.Bio,
                AvatarUrl = b.AvatarUrl,
                IsActive = b.IsActive,
                ServiceIds = b.BarberServices.Select(bs => bs.ServiceId).ToList()
            })
            .ToListAsync(ct);

        // Enrich with user contact info
        var userIds = barbers.Select(b => b.UserId).ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.PhoneNumber })
            .ToListAsync(ct);

        var map = users.ToDictionary(u => u.Id);
        return barbers.Select(b =>
        {
            if (map.TryGetValue(b.UserId, out var u))
                return b with { Email = u.Email, PhoneNumber = u.PhoneNumber };
            return b;
        }).ToList();
    }
}
