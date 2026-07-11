namespace Learnix.Infrastructure.Email.Models;

public sealed class AccountDeletedModel : BaseEmailModel
{
    public required string FirstName { get; init; }

    /// <summary>
    /// The day the data is erased on, already formatted in the recipient's language — a date is the one
    /// thing they can act on, and "30 days" from a day they cannot see is not information.
    /// </summary>
    public required string PurgeDate { get; init; }
}
