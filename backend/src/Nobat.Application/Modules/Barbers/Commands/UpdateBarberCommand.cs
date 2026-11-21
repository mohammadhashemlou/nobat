using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Barbers.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;

namespace Nobat.Application.Modules.Barbers.Commands;

public record UpdateBarberCommand : IRequest<BarberDto>
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsActive { get; init; }
    public List<Guid>? ServiceIds { get; init; }
}

public class UpdateBarberCommandValidator : AbstractValidator<UpdateBarberCommand>
{
    public UpdateBarberCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
    }
}

public class UpdateBarberHandler : IRequestHandler<UpdateBarberCommand, BarberDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateBarberHandler(IApplicationDbContext db) => _db = db;

    public async Task<BarberDto> Handle(UpdateBarberCommand request, CancellationToken ct)
    {
        var barber = await _db.Barbers.FindAsync([request.Id], ct)
            ?? throw new DomainException("آرایشگر یافت نشد", "NOT_FOUND");

        barber.FullName = request.FullName.Trim();
        barber.Bio = request.Bio?.Trim();
        barber.AvatarUrl = request.AvatarUrl;
        barber.IsActive = request.IsActive;
        barber.UpdatedAt = DateTime.UtcNow;

        if (request.ServiceIds is not null)
        {
            var existing = _db.BarberServices.Where(bs => bs.BarberId == barber.Id);
            _db.BarberServices.RemoveRange(existing);

            foreach (var serviceId in request.ServiceIds.Distinct())
            {
                _db.BarberServices.Add(new BarberService
                {
                    BarberId = barber.Id,
                    ServiceId = serviceId
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        var serviceIds = await _db.BarberServices
            .Where(bs => bs.BarberId == barber.Id)
            .Select(bs => bs.ServiceId)
            .ToListAsync(ct);

        return new BarberDto
        {
            Id = barber.Id,
            UserId = barber.UserId,
            SalonId = barber.SalonId,
            FullName = barber.FullName,
            Bio = barber.Bio,
            AvatarUrl = barber.AvatarUrl,
            IsActive = barber.IsActive,
            ServiceIds = serviceIds
        };
    }
}
