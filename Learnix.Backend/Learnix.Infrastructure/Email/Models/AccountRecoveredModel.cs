namespace Learnix.Infrastructure.Email.Models;

public sealed class AccountRecoveredModel : BaseEmailModel
{
    public required string FirstName { get; init; }
}
