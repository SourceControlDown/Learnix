using Learnix.Domain.Enums;

namespace Learnix.Application.Notifications.Abstractions;

public interface INotificationSender
{
    /// <summary>
    /// Records what happened and pushes it to whoever is online. It takes no title and no body: the wording
    /// belongs to the client, which already translates every other string it shows (ADR-NOTIF-001).
    /// </summary>
    /// <param name="parameters">
    /// Whatever the client cannot derive from the type alone — a course title, an achievement code. Null when
    /// the type says it all.
    /// </param>
    Task SendAsync(
        Guid userId,
        NotificationType type,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default);
}
