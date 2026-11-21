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

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? PhoneNumber => Principal?.FindFirstValue("phone");

    public string? FullName => Principal?.FindFirstValue(ClaimTypes.Name) ?? Principal?.FindFirstValue("name");

    public UserRole? Role
    {
        get
        {
            var role = Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");
            return Enum.TryParse<UserRole>(role, out var parsed) ? parsed : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
