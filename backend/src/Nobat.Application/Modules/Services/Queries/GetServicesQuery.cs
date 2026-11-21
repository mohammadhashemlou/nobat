using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Services.DTOs;

namespace Nobat.Application.Modules.Services.Queries;

public record GetServicesQuery(Guid SalonId, bool IncludeInactive = false) : IRequest<List<ServiceDto>>;

public class GetServicesHandler : IRequestHandler<GetServicesQuery, List<ServiceDto>>
{
    private readonly IApplicationDbContext _db;

    public GetServicesHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ServiceDto>> Handle(GetServicesQuery request, CancellationToken ct)
    {
        var query = _db.Services.AsNoTracking().Where(s => s.SalonId == request.SalonId);

        if (!request.IncludeInactive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                SalonId = s.SalonId,
                Name = s.Name,
                Description = s.Description,
                DurationMinutes = s.DurationMinutes,
                Price = s.Price,
                IsActive = s.IsActive,
                BarberIds = s.BarberServices.Select(bs => bs.BarberId).ToList()
            })
            .ToListAsync(ct);
    }
}
