namespace Learnix.Infrastructure.Constants;

/// <summary>
/// Claim names written into the access token by <see cref="Identity.JwtTokenService"/>.
/// The token format is owned by this layer; the API reads the same constants when it
/// resolves the current user, validates the principal and evaluates authorization policies.
/// </summary>
/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-008: JWT claims — standard OIDC + custom for roles
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction (email_verified claim)
/// </remarks>
public static class ClaimNames
{
    public const string Sub = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string EmailVerified = "email_verified";

    /// <summary>Value of <see cref="EmailVerified"/> for a confirmed email.</summary>
    public const string TrueValue = "true";

    /// <summary>Value of <see cref="EmailVerified"/> for an unconfirmed email.</summary>
    public const string FalseValue = "false";
}
