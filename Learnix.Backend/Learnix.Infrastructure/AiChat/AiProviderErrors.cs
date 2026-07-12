using System.Text.RegularExpressions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;

namespace Learnix.Infrastructure.AiChat;

/// <summary>
/// Turns whatever a provider SDK threw into the one thing the platform can act on: a reason and a time to
/// come back (ADR-BACK-CHAT-014).
/// <para>
/// Neither SDK exposes a status code on a typed exception the two have in common — Google.GenAI throws
/// <c>ClientError</c>/<c>ServerError</c>, Anthropic.SDK throws its own — so the classification reads the
/// text. That is coarse on purpose: the platform only ever branches on three cases, and a message that
/// mentions neither quota nor credentials belongs in the third one regardless of who wrote it.
/// </para>
/// </summary>
internal static partial class AiProviderErrors
{
    /// <summary>
    /// How long a rate-limited provider is left alone when it does not say. Short: on a free tier this is as
    /// likely to be a per-minute limit as an exhausted day, and a student locked out of a working assistant
    /// is worse than one who retries into a second refusal.
    /// </summary>
    private static readonly TimeSpan DefaultQuotaCooldown = TimeSpan.FromMinutes(5);

    public static ProviderErrorEvent Classify(Exception exception)
    {
        var message = exception.Message;

        if (IsQuota(message))
        {
            var retryAfter = ParseRetryDelay(message) ?? DefaultQuotaCooldown;
            return new ProviderErrorEvent(message, AiOutageReasons.QuotaExceeded, DateTime.UtcNow + retryAfter);
        }

        if (IsUnauthorized(message))
            return new ProviderErrorEvent(message, AiOutageReasons.Unauthorized);

        // Nothing to wait for that we know of: the next attempt finds out.
        return new ProviderErrorEvent(message, AiOutageReasons.Unavailable);
    }

    private static bool IsQuota(string message) =>
        Contains(message, "429")
        || Contains(message, "RESOURCE_EXHAUSTED")
        || Contains(message, "quota")
        || Contains(message, "rate limit")
        || Contains(message, "rate_limit")
        || Contains(message, "overloaded");

    private static bool IsUnauthorized(string message) =>
        Contains(message, "401")
        || Contains(message, "403")
        || Contains(message, "UNAUTHENTICATED")
        || Contains(message, "PERMISSION_DENIED")
        || Contains(message, "API key")
        || Contains(message, "api_key")
        || Contains(message, "authentication");

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Google answers a rate limit with <c>"retryDelay": "38s"</c> in the error body, which the SDK carries
    /// into the exception message verbatim. When it is there, it beats any guess of ours.
    /// </summary>
    private static TimeSpan? ParseRetryDelay(string message)
    {
        var match = RetryDelayPattern().Match(message);

        return match.Success && int.TryParse(match.Groups[1].Value, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    [GeneratedRegex("""retry(?:_delay|Delay|-after|After)"?\s*[:=]\s*"?(\d+)s?""", RegexOptions.IgnoreCase)]
    private static partial Regex RetryDelayPattern();
}
