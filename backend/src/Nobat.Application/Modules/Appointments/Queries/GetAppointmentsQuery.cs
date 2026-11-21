using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Appointments.DTOs;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Appointments.Queries;

/// <summary>
/// Lists appointments with flexible filters. Access rules are applied by the controller.
/// </summary>
public record GetAppointmentsQuery : IRequest<List<AppointmentDto>>
{
    public Guid? SalonId { get; init; }
    public Guid? BarberId { get; init; }
    public Guid? CustomerId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public AppointmentStatus? Status { get; init; }
    public bool UpcomingOnly { get; init; }
}

public class GetAppointmentsHandler : IRequestHandler<GetAppointmentsQuery, List<AppointmentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAppointmentsHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AppointmentDto>> Handle(GetAppointmentsQuery request, CancellationToken ct)
    {
        var query = _db.Appointments.AsNoTracking();

        if (request.SalonId.HasValue) query = query.Where(a => a.SalonId == request.SalonId);
        if (request.BarberId.HasValue) query = query.Where(a => a.BarberId == request.BarberId);
        if (request.CustomerId.HasValue) query = query.Where(a => a.CustomerId == request.CustomerId);
        if (request.Status.HasValue) query = query.Where(a => a.Status == request.Status);
        if (request.FromUtc.HasValue) query = query.Where(a => a.StartAt >= request.FromUtc);
        if (request.ToUtc.HasValue) query = query.Where(a => a.StartAt < request.ToUtc);
        if (request.UpcomingOnly) query = query.Where(a => a.StartAt >= DateTime.UtcNow);

        return await query
            .OrderBy(a => a.StartAt)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                SalonId = a.SalonId,
                BarberId = a.BarberId,
                BarberName = a.Barber.FullName,
                ServiceId = a.ServiceId,
                ServiceName = a.Service.Name,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer.FullName,
                CustomerPhone = a.Customer.PhoneNumber,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                Status = a.Status.ToString(),
                Notes = a.Notes,
                Price = a.Service.Price,
                DurationMinutes = a.Service.DurationMinutes
            })
            .ToListAsync(ct);
    }
}
