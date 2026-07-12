using System.Security.Claims;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Infrastructure.Constants;

namespace Learnix.API.Services;

/// <summary>
/// Reads current user from JWT claims (see ADR-034 for claim name mapping).
/// Registered as scoped because HttpContext is request-scoped.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimNames.Sub);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimNames.Email);

    public bool IsEmailConfirmed =>
        User?.FindFirstValue(ClaimNames.EmailVerified) == ClaimNames.TrueValue;

    public IReadOnlyList<string> GetRoles() =>
        User?.FindAll(ClaimNames.Role).Select(c => c.Value).ToList() ?? [];

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}
