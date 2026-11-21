using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Salons.DTOs;

namespace Nobat.Application.Modules.Salons.Queries;

/// <summary>دریافت اطلاعات عمومی سالن برای صفحه رزرو</summary>
public record GetPublicSalonQuery(string Slug) : IRequest<PublicSalonDto?>;

public class GetPublicSalonHandler : IRequestHandler<GetPublicSalonQuery, PublicSalonDto?>
{
    private readonly IApplicationDbContext _db;

    public GetPublicSalonHandler(IApplicationDbContext db) => _db = db;

    public async Task<PublicSalonDto?> Handle(GetPublicSalonQuery request, CancellationToken ct)
    {
        var salon = await _db.Salons
            .AsNoTracking()
            .Where(s => s.Slug == request.Slug)
            .Select(s => new PublicSalonDto
            {
                Id = s.Id,
                Name = s.Name,
                Slug = s.Slug,
                Address = s.Address,
                Phone = s.Phone,
                Description = s.Description,
                Services = s.Services
                    .Where(sv => sv.IsActive)
                    .OrderBy(sv => sv.Name)
                    .Select(sv => new PublicServiceDto
                    {
                        Id = sv.Id,
                        Name = sv.Name,
                        Description = sv.Description,
                        DurationMinutes = sv.DurationMinutes,
                        Price = sv.Price
                    }).ToList(),
                Barbers = s.Barbers
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.FullName)
                    .Select(b => new PublicBarberDto
                    {
                        Id = b.Id,
                        FullName = b.FullName,
                        Bio = b.Bio,
                        AvatarUrl = b.AvatarUrl,
                        ServiceIds = b.BarberServices.Select(bs => bs.ServiceId).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        return salon;
    }
}
