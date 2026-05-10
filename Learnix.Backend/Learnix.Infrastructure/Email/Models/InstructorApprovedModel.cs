using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

internal sealed class InstructorApprovedModel
{
    public required string FirstName { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
