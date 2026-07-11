namespace Learnix.Application.Common.Abstractions.Identity;

public interface IUserRoleService
{
    Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, IReadOnlyList<string>>> GetRolesBulkAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<int> CountUsersInRoleAsync(string role, CancellationToken cancellationToken = default);
}
