using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.DTOs;
using Nobat.Domain.Common;

namespace Nobat.Application.Modules.Identity.Handlers;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IIdentityService _identityService;

    public LoginHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();

        var (success, userId, fullName, role) = await _identityService.ValidateCredentialsAsync(
            phoneNumber, request.Password);

        if (!success || userId == Guid.Empty)
        {
            throw new DomainException("شماره موبایل یا رمز عبور نادرست است", "INVALID_CREDENTIALS");
        }

        var accessToken = await _identityService.GenerateAccessTokenAsync(userId, phoneNumber, fullName, role);
        var (refreshToken, refreshExpiresAt) = await _identityService.GenerateRefreshTokenAsync(userId);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = new UserInfo
            {
                Id = userId,
                PhoneNumber = phoneNumber,
                FullName = fullName,
                Role = role.ToString()
            }
        };
    }
}
