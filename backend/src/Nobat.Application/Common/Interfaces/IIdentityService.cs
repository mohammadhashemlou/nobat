using Nobat.Domain.Enums;

namespace Nobat.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, string? UserId, string? Error)> CreateUserAsync(
        string phoneNumber, string password, string fullName, UserRole role);

    Task<bool> PhoneNumberExistsAsync(string phoneNumber);

    Task<(bool Success, Guid UserId, string FullName, UserRole Role)> ValidateCredentialsAsync(
        string phoneNumber, string password);

    Task AddToRoleAsync(Guid userId, UserRole role);

    Task<string> GenerateAccessTokenAsync(Guid userId, string phoneNumber, string fullName, UserRole role);

    Task<(string Token, DateTime ExpiresAt)> GenerateRefreshTokenAsync(Guid userId);

    Task<bool> ValidateRefreshTokenAsync(Guid userId, string token);

    Task RevokeRefreshTokenAsync(Guid userId, string token);

    Task<(string PhoneNumber, string FullName, UserRole Role)> GetUserByIdAsync(Guid userId);
}
