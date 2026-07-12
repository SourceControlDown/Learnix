namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// Why the assistant is unavailable — as the server knows it. Only <see cref="Public"/> ever reaches a
/// student: a rejected key and a missing one are the operator's business, and telling a stranger the state
/// of our credentials buys them information and us nothing (ADR-CHAT-014).
/// </summary>
public static class AiOutageReasons
{
    /// <summary>The provider has no API key. Nothing to wait for; the deployment is misconfigured.</summary>
    public const string NotConfigured = "not_configured";

    /// <summary>Rate limit or exhausted quota — the free tier's daily budget is the usual cause.</summary>
    public const string QuotaExceeded = "quota_exceeded";

    /// <summary>The key was rejected: revoked, expired, or wrong. Waiting will not fix it.</summary>
    public const string Unauthorized = "unauthorized";

    /// <summary>Anything else: the provider is down, unreachable, or answered with something unusable.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// The reason as the client is allowed to see it. <see cref="QuotaExceeded"/> survives — the student can
    /// act on it by coming back later. Everything else collapses into <see cref="Unavailable"/>: a broken
    /// deployment is not a fact about the student's session, and the detail is in the logs where it belongs.
    /// </summary>
    public static string Public(string reason) =>
        reason == QuotaExceeded ? QuotaExceeded : Unavailable;
}
