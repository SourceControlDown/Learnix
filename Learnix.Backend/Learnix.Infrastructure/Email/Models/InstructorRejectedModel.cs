using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

internal sealed class InstructorRejectedModel
{
    public required string FirstName { get; init; }
    public string? RejectionReason { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
