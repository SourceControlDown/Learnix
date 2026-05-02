namespace Learnix.Application.Common.Abstractions.Identity;

public interface IUserRoleService
{
    Task AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);
}
