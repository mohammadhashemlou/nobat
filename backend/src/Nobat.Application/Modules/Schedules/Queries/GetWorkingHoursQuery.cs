using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Schedules.DTOs;

namespace Nobat.Application.Modules.Schedules.Queries;

public record GetWorkingHoursQuery(Guid BarberId) : IRequest<List<WorkingHoursDto>>;

public class GetWorkingHoursHandler : IRequestHandler<GetWorkingHoursQuery, List<WorkingHoursDto>>
{
    private readonly IApplicationDbContext _db;

    public GetWorkingHoursHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<WorkingHoursDto>> Handle(GetWorkingHoursQuery request, CancellationToken ct)
    {
        var items = await _db.WorkingHours
            .AsNoTracking()
            .Where(w => w.BarberId == request.BarberId)
            .OrderBy(w => w.DayOfWeek)
            .ToListAsync(ct);

        return items.Select(w => new WorkingHoursDto
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
