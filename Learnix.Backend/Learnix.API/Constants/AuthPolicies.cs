namespace Learnix.API.Constants;

/// <summary>
/// Authorization policy names. Registered in <c>Extensions/AuthenticationExtensions.cs</c>
/// and consumed by <c>[Authorize(Policy = ...)]</c> on controller actions.
/// </summary>
public static class AuthPolicies
{
    public const string EmailConfirmed = nameof(EmailConfirmed);
}
