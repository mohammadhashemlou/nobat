using FluentValidation;
using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Services.DTOs;
using Nobat.Domain.Common;

namespace Nobat.Application.Modules.Services.Commands;

public record UpdateServiceCommand : IRequest<ServiceDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
    public List<Guid>? BarberIds { get; init; }
}

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام خدمت الزامی است")
            .MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("مدت زمان باید بیشتر از صفر باشد")
            .LessThanOrEqualTo(600);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateServiceHandler(IApplicationDbContext db) => _db = db;

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken ct)
    {
        var service = await _db.Services.FindAsync([request.Id], ct)
            ?? throw new DomainException("خدمت یافت نشد", "NOT_FOUND");

        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        // Sync barber assignments if provided
        if (request.BarberIds is not null)
        {
            var existing = _db.BarberServices.Where(bs => bs.ServiceId == service.Id);
            _db.BarberServices.RemoveRange(existing);

            foreach (var barberId in request.BarberIds.Distinct())
            {
                _db.BarberServices.Add(new Domain.Entities.BarberService
                {
                    BarberId = barberId,
                    ServiceId = service.Id
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return new ServiceDto
        {
            Id = service.Id,
            SalonId = service.SalonId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            IsActive = service.IsActive,
            BarberIds = request.BarberIds ?? []
        };
    }
}
