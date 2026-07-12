using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Abstractions;

public interface IAiChatProvider
{
    /// <summary>The provider this deployment talks to, as the status endpoint reports it.</summary>
    string Name { get; }

    /// <summary>False when the provider has no API key: the chat is off, not merely out of quota.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// A provider failure arrives as a <see cref="ProviderErrorEvent"/>, never as an exception. The SSE
    /// headers are already out by the time this runs, so a throw would only sever the connection and leave
    /// the client guessing (ADR-BACK-CHAT-014).
    /// </summary>
    IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(ChatRequest request, CancellationToken cancellationToken);
}
