namespace Learnix.Infrastructure.Email.Models;

internal sealed class EmailConfirmationModel
{
    public required string FirstName { get; init; }
    public required string ConfirmationLink { get; init; }
}
