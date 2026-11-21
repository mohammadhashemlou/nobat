using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nobat.Application.Common.Interfaces;
using Nobat.Domain.Common;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;
using Nobat.Infrastructure.Data;
using Nobat.Infrastructure.Options;

namespace Nobat.Infrastructure.Common.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NobatDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        NobatDbContext dbContext,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        return await _userManager.FindByNameAsync(phoneNumber) != null;
    }

    public async Task<(bool Success, string? UserId, string? Error)> CreateUserAsync(
        string phoneNumber, string password, string fullName, UserRole role)
    {
        var user = new ApplicationUser
        {
            UserName = phoneNumber,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            Role = role,
            IsActive = true,
            PhoneNumberConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, null, error);
        }

        await _userManager.AddToRoleAsync(user, role.ToString());

        return (true, user.Id.ToString(), null);
    }

    public async Task<(bool Success, Guid UserId, string FullName, UserRole Role)> ValidateCredentialsAsync(
        string phoneNumber, string password)
    {
        var user = await _userManager.FindByNameAsync(phoneNumber);
        if (user == null || !user.IsActive)
        {
            return (false, Guid.Empty, string.Empty, UserRole.Customer);
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            return (false, Guid.Empty, string.Empty, UserRole.Customer);
        }

        return (true, user.Id, user.FullName, user.Role);
    }

    public async Task AddToRoleAsync(Guid userId, UserRole role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            await _userManager.AddToRoleAsync(user, role.ToString());
        }
    }

    public Task<string> GenerateAccessTokenAsync(Guid userId, string phoneNumber, string fullName, UserRole role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Name, fullName),
            new("phone", phoneNumber),
            new("role", role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            signingCredentials: credentials);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public async Task<(string Token, DateTime ExpiresAt)> GenerateRefreshTokenAsync(Guid userId)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var token = Convert.ToBase64String(randomBytes);
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // The user id travels with the token so refresh does not need a second lookup.
        return ($"{userId}|{token}", expiresAt);
    }

    public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);

        return refreshToken != null && refreshToken.IsActive;
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);

        if (refreshToken != null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<(string PhoneNumber, string FullName, UserRole Role)> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new DomainException("کاربر یافت نشد", "USER_NOT_FOUND");

        return (user.PhoneNumber ?? string.Empty, user.FullName, user.Role);
    }
}
