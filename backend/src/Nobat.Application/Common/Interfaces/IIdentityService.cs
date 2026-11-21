using Nobat.Domain.Enums;

namespace Nobat.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, string? UserId, string? Error)> CreateUserAsync(
        string email, string password, string fullName, UserRole role);
    Task<(bool Success, string? UserId, string? Error)> CreateUserAsync(
        string email, string password, string fullName, UserRole role, string phoneNumber);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<(bool Success, Guid UserId, string FullName, UserRole Role)> ValidateCredentialsAsync(
        string email, string password);
    Task AddToRoleAsync(Guid userId, UserRole role);
    Task<string> GenerateAccessTokenAsync(Guid userId, string email, string fullName, UserRole role);
    Task<(string Token, DateTime ExpiresAt)> GenerateRefreshTokenAsync(Guid userId);
    Task<bool> ValidateRefreshTokenAsync(Guid userId, string token);
    Task RevokeRefreshTokenAsync(Guid userId, string token);
    Task<(string Email, string FullName, UserRole Role)> GetUserByIdAsync(Guid userId);
}
