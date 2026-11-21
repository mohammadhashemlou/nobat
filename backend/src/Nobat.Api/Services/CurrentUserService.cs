using System.Security.Claims;
using Nobat.Application.Common.Interfaces;
using Nobat.Domain.Enums;

namespace Nobat.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("email");

    public string? FullName => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("name");

    public UserRole? Role
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("role");
            return Enum.TryParse<UserRole>(role, out var parsed) ? parsed : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
