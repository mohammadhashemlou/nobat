using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Barbers.DTOs;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Barbers.Commands;

public record CreateBarberCommand : IRequest<BarberDto>
{
    public Guid SalonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public List<Guid> ServiceIds { get; init; } = [];
}

public class CreateBarberCommandValidator : AbstractValidator<CreateBarberCommand>
{
    public CreateBarberCommandValidator()
    {
        RuleFor(x => x.SalonId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Must(p => p.Any(char.IsDigit))
            .Must(p => p.Any(char.IsUpper));
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
    }
}

public class CreateBarberHandler : IRequestHandler<CreateBarberCommand, BarberDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IIdentityService _identity;

    public CreateBarberHandler(IApplicationDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<BarberDto> Handle(CreateBarberCommand request, CancellationToken ct)
    {
        var salonExists = await _db.Salons.AnyAsync(s => s.Id == request.SalonId, ct);
        if (!salonExists)
            throw new DomainException("سالن یافت نشد", "NOT_FOUND");

        if (await _identity.UserExistsByEmailAsync(request.Email))
            throw new DomainException("کاربری با این ایمیل قبلاً ثبت شده است", "EMAIL_EXISTS");

        // Create the barber's user account
        var (success, userIdStr, error) = await _identity.CreateUserAsync(
            request.Email, request.Password, request.FullName, UserRole.Barber, request.PhoneNumber);

        if (!success || userIdStr is null)
            throw new DomainException(error ?? "خطا در ایجاد حساب آرایشگر", "REGISTRATION_FAILED");

        var userId = Guid.Parse(userIdStr);

        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SalonId = request.SalonId,
            FullName = request.FullName.Trim(),
            Bio = request.Bio?.Trim(),
            AvatarUrl = request.AvatarUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Barbers.Add(barber);

        foreach (var serviceId in request.ServiceIds.Distinct())
        {
            _db.BarberServices.Add(new BarberService
            {
                BarberId = barber.Id,
                ServiceId = serviceId
            });
        }

        await _db.SaveChangesAsync(ct);

        return new BarberDto
        {
            Id = barber.Id,
            UserId = barber.UserId,
            SalonId = barber.SalonId,
            FullName = barber.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Bio = barber.Bio,
            AvatarUrl = barber.AvatarUrl,
            IsActive = barber.IsActive,
            ServiceIds = request.ServiceIds.Distinct().ToList()
        };
    }
}
