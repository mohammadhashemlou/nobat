using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Appointments.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Appointments.Commands;

public record CreateAppointmentCommand : IRequest<AppointmentDto>
{
    public Guid BarberId { get; init; }
    public Guid ServiceId { get; init; }
    public DateTime StartAtUtc { get; init; }
    public string? Notes { get; init; }
    /// <summary>Admin-only: book on behalf of a specific customer.</summary>
    public Guid? CustomerId { get; init; }
}

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.BarberId).NotEmpty().WithMessage("آرایشگر را انتخاب کنید");
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("خدمت را انتخاب کنید");
        RuleFor(x => x.StartAtUtc)
            .NotEmpty().WithMessage("زمان نوبت الزامی است")
            .Must(d => d.Kind == DateTimeKind.Utc)
            .WithMessage("زمان باید به صورت UTC ارسال شود");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateAppointmentHandler : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IBusinessTimeProvider _time;
    private readonly IDbConstraintDetector _constraints;

    public CreateAppointmentHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IBusinessTimeProvider time,
        IDbConstraintDetector constraints)
    {
        _db = db;
        _currentUser = currentUser;
        _time = time;
        _constraints = constraints;
    }

    public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        var service = await _db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct)
            ?? throw new DomainException("خدمت یافت نشد یا غیرفعال است", "NOT_FOUND");

        var barber = await _db.Barbers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BarberId && b.IsActive, ct)
            ?? throw new DomainException("آرایشگر یافت نشد یا غیرفعال است", "NOT_FOUND");

        if (barber.SalonId != service.SalonId)
            throw new DomainException("خدمت و آرایشگر متعلق به یک سالن نیستند", "INVALID_SALON");

        var offersService = await _db.BarberServices
            .AnyAsync(bs => bs.BarberId == barber.Id && bs.ServiceId == service.Id, ct);
        if (!offersService)
            throw new DomainException("این آرایشگر این خدمت را ارائه نمی‌دهد", "SERVICE_NOT_OFFERED");

        // Resolve customer
        var customerId = request.CustomerId ?? _currentUser.UserId
            ?? throw new DomainException("شناسه کاربر یافت نشد", "UNAUTHORIZED");

        if (request.CustomerId.HasValue && _currentUser.Role != UserRole.Admin)
            throw new DomainException("فقط مدیر می‌تواند برای مشتری دیگر نوبت ثبت کند", "FORBIDDEN");

        var customer = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == customerId, ct)
            ?? throw new DomainException("مشتری یافت نشد", "NOT_FOUND");

        var startUtc = request.StartAtUtc;
        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        if (startUtc < _time.UtcNow)
            throw new DomainException("زمان انتخاب‌شده گذشته است", "PAST_TIME");

        // Validate against working hours + holidays for the local day
        var tz = _time.TimeZone;
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, tz);
        var date = DateOnly.FromDateTime(localStart);

        if (DateOnly.FromDateTime(localEnd) != date)
            throw new DomainException("نوبت نباید از نیمه‌شب عبور کند", "INVALID_TIME");

        var isHoliday = await _db.Holidays
            .AnyAsync(h => h.SalonId == barber.SalonId && h.Date == date, ct);
        if (isHoliday)
            throw new DomainException("این روز تعطیل است", "HOLIDAY");

        var dow = ToIranianDayOfWeek(localStart.DayOfWeek);
        var workingHours = await _db.WorkingHours
            .FirstOrDefaultAsync(w => w.BarberId == barber.Id && w.DayOfWeek == dow, ct);

        if (workingHours is null)
            throw new DomainException("آرایشگر در این روز فعالیت ندارد", "NOT_WORKING_DAY");

        var startTime = TimeOnly.FromDateTime(localStart);
        var endTime = TimeOnly.FromDateTime(localEnd);

        if (startTime < workingHours.StartTime || endTime > workingHours.EndTime)
            throw new DomainException("زمان انتخابی خارج از ساعات کاری است", "OUTSIDE_WORKING_HOURS");

        if (workingHours.BreakStart.HasValue && workingHours.BreakEnd.HasValue
            && startTime < workingHours.BreakEnd && endTime > workingHours.BreakStart)
        {
            throw new DomainException("زمان انتخابی با استراحت آرایشگر تداخل دارد", "BREAK_OVERLAP");
        }

        // Application-level overlap check (fast, friendly error)
        var hasConflict = await _db.Appointments.AnyAsync(a =>
            a.BarberId == barber.Id
            && a.Status != AppointmentStatus.Cancelled
            && a.Status != AppointmentStatus.NoShow
            && a.StartAt < endUtc
            && a.EndAt > startUtc, ct);

        if (hasConflict)
            throw new DomainException("این بازه زمانی قبلاً رزرو شده است", "SLOT_ALREADY_BOOKED");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            SalonId = barber.SalonId,
            BarberId = barber.Id,
            ServiceId = service.Id,
            CustomerId = customerId,
            StartAt = startUtc,
            EndAt = endUtc,
            Status = AppointmentStatus.Confirmed,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        appointment.StatusHistory.Add(new AppointmentStatusHistory
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            Status = AppointmentStatus.Confirmed,
            ChangedByUserId = _currentUser.UserId ?? customerId,
            CreatedAt = DateTime.UtcNow
        });

        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (_constraints.IsOverlapViolation(ex))
        {
            // Database-level exclusion constraint fired (race condition)
            throw new DomainException("این بازه زمانی همین لحظه رزرو شد. لطفاً زمان دیگری انتخاب کنید", "SLOT_ALREADY_BOOKED");
        }

        return new AppointmentDto
        {
            Id = appointment.Id,
            SalonId = appointment.SalonId,
            BarberId = barber.Id,
            BarberName = barber.FullName,
            ServiceId = service.Id,
            ServiceName = service.Name,
            CustomerId = customerId,
            CustomerName = customer.FullName,
            CustomerPhone = customer.PhoneNumber,
            StartAt = startUtc,
            EndAt = endUtc,
            Status = appointment.Status.ToString(),
            Notes = appointment.Notes,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes
        };
    }

    private static Domain.Enums.DayOfWeek ToIranianDayOfWeek(System.DayOfWeek dow) => dow switch
    {
        System.DayOfWeek.Saturday => Domain.Enums.DayOfWeek.Saturday,
        System.DayOfWeek.Sunday => Domain.Enums.DayOfWeek.Sunday,
        System.DayOfWeek.Monday => Domain.Enums.DayOfWeek.Monday,
        System.DayOfWeek.Tuesday => Domain.Enums.DayOfWeek.Tuesday,
        System.DayOfWeek.Wednesday => Domain.Enums.DayOfWeek.Wednesday,
        System.DayOfWeek.Thursday => Domain.Enums.DayOfWeek.Thursday,
        _ => Domain.Enums.DayOfWeek.Friday
    };
}
