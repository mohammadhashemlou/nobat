using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Schedules.DTOs;

namespace Nobat.Application.Modules.Schedules.Queries;

public record GetHolidaysQuery(Guid SalonId) : IRequest<List<HolidayDto>>;

public class GetHolidaysHandler : IRequestHandler<GetHolidaysQuery, List<HolidayDto>>
{
    private readonly IApplicationDbContext _db;

    public GetHolidaysHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<HolidayDto>> Handle(GetHolidaysQuery request, CancellationToken ct)
    {
        return await _db.Holidays
            .AsNoTracking()
            .Where(h => h.SalonId == request.SalonId)
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Date = h.Date,
                Description = h.Description
            })
            .ToListAsync(ct);
    }
}

public record DeleteHolidayCommand(Guid Id) : IRequest<Unit>;

public class DeleteHolidayHandler : IRequestHandler<DeleteHolidayCommand, Unit>
{
    private readonly IApplicationDbContext _db;

    public DeleteHolidayHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteHolidayCommand request, CancellationToken ct)
    {
        var holiday = await _db.Holidays.FindAsync([request.Id], ct)
            ?? throw new Domain.Common.DomainException("تعطیلی یافت نشد", "NOT_FOUND");

        _db.Holidays.Remove(holiday);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
