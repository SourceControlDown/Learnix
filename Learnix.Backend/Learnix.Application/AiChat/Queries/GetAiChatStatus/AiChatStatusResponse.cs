namespace Learnix.Application.AiChat.Queries.GetAiChatStatus;

/// <param name="Reason">Null when available; otherwise one of <c>AiOutageReasons</c>.</param>
/// <param name="RetryAtUtc">When the assistant is expected back, when that is known.</param>
public sealed record AiChatStatusResponse(
    bool Available,
    string Provider,
    string? Reason,
    DateTime? RetryAtUtc);
