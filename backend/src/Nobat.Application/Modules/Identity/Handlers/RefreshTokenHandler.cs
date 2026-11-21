using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.DTOs;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Identity.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Decode the refresh token to extract the user ID
        // The token format is: {userId}|{randomToken}
        var parts = request.RefreshToken.Split('|', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
        {
            throw new Nobat.Domain.Common.DomainException("توکن بازنشانی نامعتبر است", "INVALID_REFRESH_TOKEN");
        }

        var isValid = await _identityService.ValidateRefreshTokenAsync(userId, parts[1]);
        if (!isValid)
        {
            throw new Nobat.Domain.Common.DomainException("توکن بازنشانی نامعتبر یا منقضی شده است", "INVALID_REFRESH_TOKEN");
        }

        // Revoke old token
        await _identityService.RevokeRefreshTokenAsync(userId, parts[1]);

        // Get user info
        var (email, fullName, role) = await _identityService.GetUserByIdAsync(userId);

        // Generate new tokens
        var accessToken = await _identityService.GenerateAccessTokenAsync(userId, email, fullName, role);
        var (newRefreshToken, refreshExpiresAt) = await _identityService.GenerateRefreshTokenAsync(userId);

        return new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }
}
