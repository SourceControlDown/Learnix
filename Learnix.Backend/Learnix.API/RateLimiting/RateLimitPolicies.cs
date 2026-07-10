namespace Learnix.API.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>
    /// Strict per-IP limit for sensitive auth endpoints:
    /// register, login, google login, forgot-password, reset-password,
    /// resend-confirmation, confirm-email.
    /// 5 requests per 15 minutes per IP.
    /// </summary>
    public const string AuthStrict = "auth-strict";

    /// <summary>
    /// Per-user limit for the platform-wide AI assistant.
    /// 20 requests per hour per authenticated user.
    /// </summary>
    public const string AiChatPlatform = "ai-chat-platform";

    /// <summary>
    /// Per-user limit for the course tutor. Its own budget, and a larger one: working through a topic
    /// with a tutor is dozens of turns, and course discovery must not spend it.
    /// 60 requests per hour per authenticated user.
    /// </summary>
    public const string AiChatTutor = "ai-chat-tutor";

    /// <summary>
    /// Per-user limit for test attempt operations (start, submit) to prevent bot spamming.
    /// 30 requests per minute per authenticated user.
    /// </summary>
    public const string TestAttempts = "test-attempts";

    /// <summary>
    /// Limit for creating payment checkouts to avoid Stripe API abuse.
    /// 5 requests per minute per user.
    /// </summary>
    public const string Payments = "payments";

    /// <summary>
    /// Limit for requesting upload signed URLs to prevent storage DoS.
    /// 20 requests per minute per user.
    /// </summary>
    public const string Uploads = "uploads";

    /// <summary>
    /// Limit for sending chat messages to prevent chat spamming.
    /// 30 requests per minute per user.
    /// </summary>
    public const string ChatMessages = "chat-messages";
}
