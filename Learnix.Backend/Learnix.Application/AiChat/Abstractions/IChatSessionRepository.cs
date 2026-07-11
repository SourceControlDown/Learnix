using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Abstractions;

public interface IChatSessionRepository
{
    /// <summary>Returns the session for the scope, or null when the user has never written in it.</summary>
    Task<ChatSession?> GetByScopeAsync(Guid userId, ChatScope scope, CancellationToken ct = default);

    /// <summary>Upserts atomically: a concurrent first message cannot fork the history.</summary>
    Task<ChatSession> GetOrCreateAsync(Guid userId, ChatScope scope, CancellationToken ct = default);

    /// <summary>
    /// Appends and trims to the newest <paramref name="storedMessagesLimit"/> messages in one atomic update.
    /// </summary>
    Task AppendMessagesAsync(
        string sessionId,
        IEnumerable<ChatMessage> messages,
        int storedMessagesLimit,
        CancellationToken ct = default);

    Task DeleteAsync(Guid userId, ChatScope scope, CancellationToken ct = default);

    /// <summary>
    /// Every session the user has, in every scope. Chat history is as personal as it gets, so an account
    /// being purged must not leave it behind in Mongo (ADR-USERS-001).
    /// </summary>
    Task<long> DeleteAllForUserAsync(Guid userId, CancellationToken ct = default);
}
