namespace Learnix.Application.AiChat.Abstractions.Models;

/// <summary>
/// Everything a provider needs for one turn. The system prompt travels with the request because it
/// depends on the scope — providers no longer reach for a shared constant.
/// </summary>
public sealed record ChatRequest(
    IReadOnlyList<ChatMessage> Conversation,
    IReadOnlyList<ToolDefinition> Tools,
    string SystemPrompt);
