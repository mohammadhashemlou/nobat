using FluentValidation;
using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Salons.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;

namespace Nobat.Application.Modules.Salons.Commands;

public record UpdateSalonCommand : IRequest<SalonDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Description { get; init; }
}

public class UpdateSalonCommandValidator : AbstractValidator<UpdateSalonCommand>
{
    public UpdateSalonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام سالن الزامی است")
            .MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateSalonHandler : IRequestHandler<UpdateSalonCommand, SalonDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateSalonHandler(IApplicationDbContext db) => _db = db;

    public async Task<SalonDto> Handle(UpdateSalonCommand request, CancellationToken ct)
    {
        var salon = await _db.Salons.FindAsync([request.Id], ct)
            ?? throw new DomainException("سالن یافت نشد", "NOT_FOUND");

        salon.Name = request.Name;
        salon.Address = request.Address;
        salon.Phone = request.Phone;
        salon.Description = request.Description;
        salon.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new SalonDto
        {
            Id = salon.Id,
            Name = salon.Name,
            Slug = salon.Slug,
            Address = salon.Address,
            Phone = salon.Phone,
            Description = salon.Description,
            DefaultTimezone = salon.DefaultTimezone
        };
    }
}
