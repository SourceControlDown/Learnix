namespace Learnix.Infrastructure.Email.Models;

internal sealed class UserRoleChangedModel
{
    public required string FirstName { get; init; }
    public required string Role { get; init; }
    public required bool Assigned { get; init; }
}
