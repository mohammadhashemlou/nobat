using Nobat.Domain.Enums;

namespace Nobat.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
