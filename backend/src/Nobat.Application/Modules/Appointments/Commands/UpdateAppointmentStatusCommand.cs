using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Appointments.Commands;

public record UpdateAppointmentStatusCommand(Guid Id, AppointmentStatus Status) : IRequest<Unit>;

public class UpdateAppointmentStatusHandler : IRequestHandler<UpdateAppointmentStatusCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAppointmentStatusHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateAppointmentStatusCommand request, CancellationToken ct)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new DomainException("نوبت یافت نشد", "NOT_FOUND");

        // Customers may only cancel their own appointments
        if (_currentUser.Role == UserRole.Customer)
        {
            if (appointment.CustomerId != _currentUser.UserId)
                throw new DomainException("دسترسی غیرمجاز", "FORBIDDEN");
            if (request.Status != AppointmentStatus.Cancelled)
                throw new DomainException("مشتری فقط می‌تواند نوبت را لغو کند", "FORBIDDEN");
        }

        if (appointment.Status == request.Status)
            return Unit.Value;

        appointment.Status = request.Status;
        appointment.UpdatedAt = DateTime.UtcNow;
        if (request.Status == AppointmentStatus.Cancelled)
            appointment.CancelledAt = DateTime.UtcNow;

        _db.AppointmentStatusHistory.Add(new AppointmentStatusHistory
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            Status = request.Status,
            ChangedByUserId = _currentUser.UserId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public record CancelAppointmentCommand(Guid Id) : IRequest<Unit>;

public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, Unit>
{
    private readonly IMediator _mediator;

    public CancelAppointmentHandler(IMediator mediator) => _mediator = mediator;

    public Task<Unit> Handle(CancelAppointmentCommand request, CancellationToken ct)
        => _mediator.Send(new UpdateAppointmentStatusCommand(request.Id, AppointmentStatus.Cancelled), ct);
}
