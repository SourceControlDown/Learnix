namespace Learnix.Application.Common.Options;

public sealed class AiChatOptions
{
    public string Provider { get; init; } = "Anthropic";

    /// <summary>
    /// How many messages a session keeps. Older ones are dropped on write; the session itself never ends.
    /// Counts tool results and tool-calling assistant turns too, so it is not the number of visible bubbles.
    /// </summary>
    public int StoredMessagesLimit { get; init; } = 50;

    /// <summary>How many of the stored messages are replayed to the provider (ADR-BACK-CHAT-005).</summary>
    public int ContextWindowSize { get; init; } = 20;
}
