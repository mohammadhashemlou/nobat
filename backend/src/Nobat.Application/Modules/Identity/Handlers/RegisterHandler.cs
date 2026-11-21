using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.DTOs;
using Nobat.Domain.Enums;

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
        if (await _identityService.UserExistsByEmailAsync(request.Email))
        {
            throw new Nobat.Domain.Common.DomainException("کاربری با این ایمیل قبلاً ثبت‌نام کرده است", "EMAIL_EXISTS");
        }

        var (success, userId, error) = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? await _identityService.CreateUserAsync(request.Email, request.Password, request.FullName, request.Role)
            : await _identityService.CreateUserAsync(request.Email, request.Password, request.FullName, request.Role, request.PhoneNumber);

        if (!success || userId == null)
        {
            throw new Nobat.Domain.Common.DomainException(error ?? "خطا در ثبت‌نام کاربر", "REGISTRATION_FAILED");
        }

        return new RegisterResponse
        {
            UserId = Guid.Parse(userId),
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role
        };
    }
}
