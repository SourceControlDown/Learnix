namespace Learnix.Infrastructure.Outbox.Payloads.Users;

/// <param name="PurgeAfterUtc">
/// The date the account is erased on, as stored on the user. A fact, not a policy: the email must name the
/// day this person was actually promised, whatever the recovery window is set to by the time it is sent.
/// </param>
public sealed record SendAccountDeletedEmailPayload(
    string ToEmail,
    string FirstName,
    DateTime PurgeAfterUtc,
    string Language);
