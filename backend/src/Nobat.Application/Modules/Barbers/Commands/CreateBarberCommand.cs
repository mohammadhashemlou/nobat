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
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
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
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches(@"^09\d{9}$").WithMessage("شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد")
            .Must(p => p.Any(char.IsDigit)).WithMessage("رمز عبور باید حداقل یک عدد داشته باشد")
            .Must(p => p.Any(char.IsUpper)).WithMessage("رمز عبور باید حداقل یک حرف بزرگ داشته باشد");
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
        if (!await _db.Salons.AnyAsync(s => s.Id == request.SalonId, ct))
        {
            throw new DomainException("سالن یافت نشد", "NOT_FOUND");
        }

        var phoneNumber = request.PhoneNumber.Trim();

        if (await _identity.PhoneNumberExistsAsync(phoneNumber))
        {
            throw new DomainException("این شماره موبایل قبلاً ثبت شده است", "PHONE_EXISTS");
        }

        var (success, userIdStr, error) = await _identity.CreateUserAsync(
            phoneNumber, request.Password, request.FullName.Trim(), UserRole.Barber);

        if (!success || userIdStr is null)
        {
            throw new DomainException(error ?? "خطا در ایجاد حساب آرایشگر", "REGISTRATION_FAILED");
        }

        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(userIdStr),
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
            PhoneNumber = phoneNumber,
            Bio = barber.Bio,
            AvatarUrl = barber.AvatarUrl,
            IsActive = barber.IsActive,
            ServiceIds = request.ServiceIds.Distinct().ToList()
        };
    }
}
