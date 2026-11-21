using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Salons.DTOs;

namespace Nobat.Application.Modules.Salons.Queries;

/// <summary>دریافت اطلاعات سالن جاری (پنل مدیریت)</summary>
public record GetSalonQuery(Guid SalonId) : IRequest<SalonDto?>;

public class GetSalonHandler : IRequestHandler<GetSalonQuery, SalonDto?>
{
    private readonly IApplicationDbContext _db;

    public GetSalonHandler(IApplicationDbContext db) => _db = db;

    public async Task<SalonDto?> Handle(GetSalonQuery request, CancellationToken ct)
    {
        return await _db.Salons
            .AsNoTracking()
            .Where(s => s.Id == request.SalonId)
            .Select(s => new SalonDto
            {
                Id = s.Id,
                Name = s.Name,
                Slug = s.Slug,
                Address = s.Address,
                Phone = s.Phone,
                Description = s.Description,
                DefaultTimezone = s.DefaultTimezone
            })
            .FirstOrDefaultAsync(ct);
    }
}
