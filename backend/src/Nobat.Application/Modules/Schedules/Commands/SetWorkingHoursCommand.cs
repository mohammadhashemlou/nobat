using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Schedules.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;

namespace Nobat.Application.Modules.Schedules.Commands;

public record SetWorkingHoursCommand : IRequest<List<WorkingHoursDto>>
{
    public Guid BarberId { get; init; }
    public List<WorkingHoursItem> Items { get; init; } = [];
}

public record WorkingHoursItem
{
    /// <summary>0 = Saturday ... 6 = Friday</summary>
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? BreakStart { get; init; }
    public string? BreakEnd { get; init; }
}

public class SetWorkingHoursCommandValidator : AbstractValidator<SetWorkingHoursCommand>
{
    public SetWorkingHoursCommandValidator()
    {
        RuleFor(x => x.BarberId).NotEmpty();

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DayOfWeek).InclusiveBetween(0, 6)
                .WithMessage("روز هفته نامعتبر است");
            item.RuleFor(i => i.StartTime)
                .Must(BeValidTime).WithMessage("ساعت شروع نامعتبر است");
            item.RuleFor(i => i.EndTime)
                .Must(BeValidTime).WithMessage("ساعت پایان نامعتبر است");
            item.RuleFor(i => i)
                .Must(i => !TimeOnly.TryParse(i.StartTime, out var s)
                           || !TimeOnly.TryParse(i.EndTime, out var e)
                           || s < e)
                .WithMessage("ساعت شروع باید قبل از ساعت پایان باشد");
            item.RuleFor(i => i.BreakStart)
                .Must(t => string.IsNullOrWhiteSpace(t) || BeValidTime(t))
                .WithMessage("ساعت شروع استراحت نامعتبر است");
            item.RuleFor(i => i.BreakEnd)
                .Must(t => string.IsNullOrWhiteSpace(t) || BeValidTime(t))
                .WithMessage("ساعت پایان استراحت نامعتبر است");
        });

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.DayOfWeek).Distinct().Count() == items.Count)
            .WithMessage("برای هر روز فقط یک بازه کاری مجاز است");
    }

    private static bool BeValidTime(string? value) =>
        !string.IsNullOrWhiteSpace(value) && TimeOnly.TryParse(value, out _);
}

public class SetWorkingHoursHandler : IRequestHandler<SetWorkingHoursCommand, List<WorkingHoursDto>>
{
    private readonly IApplicationDbContext _db;

    public SetWorkingHoursHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<WorkingHoursDto>> Handle(SetWorkingHoursCommand request, CancellationToken ct)
    {
        var barberExists = await _db.Barbers.AnyAsync(b => b.Id == request.BarberId, ct);
        if (!barberExists)
            throw new DomainException("آرایشگر یافت نشد", "NOT_FOUND");

        // Replace all working hours for this barber
        var existing = _db.WorkingHours.Where(w => w.BarberId == request.BarberId);
        _db.WorkingHours.RemoveRange(existing);

        var created = new List<WorkingHours>();

        foreach (var item in request.Items)
        {
            var wh = new WorkingHours
            {
                Id = Guid.NewGuid(),
                BarberId = request.BarberId,
                DayOfWeek = (Domain.Enums.DayOfWeek)item.DayOfWeek,
                StartTime = TimeOnly.Parse(item.StartTime),
                EndTime = TimeOnly.Parse(item.EndTime),
                BreakStart = string.IsNullOrWhiteSpace(item.BreakStart) ? null : TimeOnly.Parse(item.BreakStart),
                BreakEnd = string.IsNullOrWhiteSpace(item.BreakEnd) ? null : TimeOnly.Parse(item.BreakEnd),
                CreatedAt = DateTime.UtcNow
            };
            created.Add(wh);
            _db.WorkingHours.Add(wh);
        }

        await _db.SaveChangesAsync(ct);

        return created
            .OrderBy(w => w.DayOfWeek)
            .Select(w => new WorkingHoursDto
            {
                Id = w.Id,
                BarberId = w.BarberId,
                DayOfWeek = w.DayOfWeek.ToString(),
                DayOfWeekValue = (int)w.DayOfWeek,
                StartTime = w.StartTime.ToString("HH:mm"),
                EndTime = w.EndTime.ToString("HH:mm"),
                BreakStart = w.BreakStart?.ToString("HH:mm"),
                BreakEnd = w.BreakEnd?.ToString("HH:mm")
            }).ToList();
    }
}
