using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Services.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;

namespace Nobat.Application.Modules.Services.Commands;

public record CreateServiceCommand : IRequest<ServiceDto>
{
    public Guid SalonId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
}

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.SalonId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام خدمت الزامی است")
            .MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("مدت زمان باید بیشتر از صفر باشد")
            .LessThanOrEqualTo(600).WithMessage("مدت زمان نمی‌تواند بیشتر از ۶۰۰ دقیقه باشد");
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد");
    }
}

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IApplicationDbContext _db;

    public CreateServiceHandler(IApplicationDbContext db) => _db = db;

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken ct)
    {
        var salonExists = await _db.Salons.AnyAsync(s => s.Id == request.SalonId, ct);
        if (!salonExists)
            throw new DomainException("سالن یافت نشد", "NOT_FOUND");

        var service = new Service
        {
            Id = Guid.NewGuid(),
            SalonId = request.SalonId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Services.Add(service);
        await _db.SaveChangesAsync(ct);

        return new ServiceDto
        {
            Id = service.Id,
            SalonId = service.SalonId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            IsActive = service.IsActive
        };
    }
}
