namespace Learnix.Infrastructure.Email.Models;

internal sealed class PasswordResetModel
{
    public required string FirstName { get; init; }
    public required string ResetLink { get; init; }
}
