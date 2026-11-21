using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Schedules.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;

namespace Nobat.Application.Modules.Schedules.Commands;

public record AddHolidayCommand : IRequest<HolidayDto>
{
    public Guid SalonId { get; init; }
    public DateOnly Date { get; init; }
    public string? Description { get; init; }
}

public class AddHolidayCommandValidator : AbstractValidator<AddHolidayCommand>
{
    public AddHolidayCommandValidator()
    {
        RuleFor(x => x.SalonId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class AddHolidayHandler : IRequestHandler<AddHolidayCommand, HolidayDto>
{
    private readonly IApplicationDbContext _db;

    public AddHolidayHandler(IApplicationDbContext db) => _db = db;

    public async Task<HolidayDto> Handle(AddHolidayCommand request, CancellationToken ct)
    {
        var salonExists = await _db.Salons.AnyAsync(s => s.Id == request.SalonId, ct);
        if (!salonExists)
            throw new DomainException("سالن یافت نشد", "NOT_FOUND");

        var duplicate = await _db.Holidays
            .AnyAsync(h => h.SalonId == request.SalonId && h.Date == request.Date, ct);
        if (duplicate)
            throw new DomainException("برای این تاریخ قبلاً تعطیلی ثبت شده است", "DUPLICATE_HOLIDAY");

        var holiday = new Holiday
        {
            Id = Guid.NewGuid(),
            SalonId = request.SalonId,
            Date = request.Date,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Holidays.Add(holiday);
        await _db.SaveChangesAsync(ct);

        return new HolidayDto
        {
            Id = holiday.Id,
            Date = holiday.Date,
            Description = holiday.Description
        };
    }
}
