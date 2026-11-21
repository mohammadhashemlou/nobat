using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.DTOs;
using Nobat.Domain.Common;

namespace Nobat.Application.Modules.Identity.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IIdentityService _identityService;

    public RegisterHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();

        if (await _identityService.PhoneNumberExistsAsync(phoneNumber))
        {
            throw new DomainException("این شماره موبایل قبلاً ثبت شده است", "PHONE_EXISTS");
        }

        var (success, userId, error) = await _identityService.CreateUserAsync(
            phoneNumber, request.Password, request.FullName.Trim(), request.Role);

        if (!success || userId is null)
        {
            throw new DomainException(error ?? "خطا در ثبت‌نام کاربر", "REGISTRATION_FAILED");
        }

        return new RegisterResponse
        {
            UserId = Guid.Parse(userId),
            PhoneNumber = phoneNumber,
            FullName = request.FullName.Trim(),
            Role = request.Role
        };
    }
}
