using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Appointments.DTOs;
using Nobat.Application.Modules.Appointments.Services;
using Nobat.Domain.Common;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Appointments.Queries;

public record GetAvailabilityQuery(
    Guid BarberId,
    Guid ServiceId,
    DateOnly Date) : IRequest<AvailabilityDto>;

public class GetAvailabilityHandler : IRequestHandler<GetAvailabilityQuery, AvailabilityDto>
{
    private const int SlotStepMinutes = 15;
    private const int MinLeadTimeMinutes = 60;

    private readonly IApplicationDbContext _db;
    private readonly IBusinessTimeProvider _time;

    public GetAvailabilityHandler(IApplicationDbContext db, IBusinessTimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<AvailabilityDto> Handle(GetAvailabilityQuery request, CancellationToken ct)
    {
        var service = await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, ct)
            ?? throw new DomainException("خدمت یافت نشد", "NOT_FOUND");

        var barber = await _db.Barbers
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BarberId, ct)
            ?? throw new DomainException("آرایشگر یافت نشد", "NOT_FOUND");

        // Barber must offer this service
        var offersService = await _db.BarberServices
            .AnyAsync(bs => bs.BarberId == barber.Id && bs.ServiceId == service.Id, ct);
        if (!offersService)
            throw new DomainException("این آرایشگر این خدمت را ارائه نمی‌دهد", "SERVICE_NOT_OFFERED");

        var dto = new AvailabilityDto
        {
            BarberId = barber.Id,
            ServiceId = service.Id,
            Date = request.Date
        };

        // Holiday check
        var holiday = await _db.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.SalonId == barber.SalonId && h.Date == request.Date, ct);

        if (holiday is not null)
        {
            dto = dto with { IsHoliday = true, HolidayDescription = holiday.Description };
            return dto;
        }

        // Day of week (Iranian calendar: Saturday = 0)
        var dow = ToIranianDayOfWeek(request.Date.DayOfWeek);

        var workingHours = await _db.WorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.BarberId == barber.Id && w.DayOfWeek == dow, ct);

        if (workingHours is null)
            return dto; // not a working day

        // Local day boundaries -> UTC window for fetching busy appointments
        var tz = _time.TimeZone;
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), tz);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.Date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), tz);

        var busy = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.BarberId == barber.Id
                        && a.StartAt < dayEndUtc
                        && a.EndAt > dayStartUtc
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Status != AppointmentStatus.NoShow)
            .Select(a => new { a.StartAt, a.EndAt })
            .ToListAsync(ct);

        var calculator = new AvailabilityCalculator();
        var slots = calculator.Calculate(
            request.Date,
            workingHours.StartTime,
            workingHours.EndTime,
            workingHours.BreakStart,
            workingHours.BreakEnd,
            service.DurationMinutes,
            SlotStepMinutes,
            busy.Select(b => (b.StartAt, b.EndAt)),
            _time.UtcNow,
            MinLeadTimeMinutes,
            tz);

        return dto with
        {
            IsWorkingDay = true,
            Slots = slots.Select(s => new TimeSlotDto
            {
                StartUtc = s.StartUtc,
                EndUtc = s.EndUtc,
                LocalStart = TimeZoneInfo.ConvertTimeFromUtc(s.StartUtc, tz).ToString("HH:mm"),
                LocalEnd = TimeZoneInfo.ConvertTimeFromUtc(s.EndUtc, tz).ToString("HH:mm")
            }).ToList()
        };
    }

    /// <summary>Maps .NET DayOfWeek to Iranian week (Saturday = 0 ... Friday = 6).</summary>
    internal static Domain.Enums.DayOfWeek ToIranianDayOfWeek(System.DayOfWeek dow) => dow switch
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
