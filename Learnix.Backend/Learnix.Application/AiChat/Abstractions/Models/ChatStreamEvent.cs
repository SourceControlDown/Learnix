namespace Learnix.Application.AiChat.Abstractions.Models;

public abstract record ChatStreamEvent;

public sealed record TextDeltaEvent(string Content) : ChatStreamEvent;

public sealed record ToolUseStartEvent(string CallId, string ToolName) : ChatStreamEvent;

public sealed record ToolUseEndEvent(string CallId, string ToolName, string ArgumentsJson) : ChatStreamEvent;

public sealed record MessageEndEvent(string FinishReason) : ChatStreamEvent;

/// <param name="Code">One of <see cref="Constants.AiOutageReasons"/> — what kind of failure this was.</param>
/// <param name="RetryAtUtc">When the provider is worth calling again, when it says so.</param>
public sealed record ProviderErrorEvent(string Message, string Code, DateTime? RetryAtUtc = null)
    : ChatStreamEvent;
