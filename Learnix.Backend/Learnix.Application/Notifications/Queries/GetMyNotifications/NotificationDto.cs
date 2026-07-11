namespace Learnix.Application.Notifications.Queries.GetMyNotifications;

/// <param name="Parameters">
/// The values the client's own translation needs — a course title, an achievement code. Null when the type
/// alone is the whole message (ADR-NOTIF-001).
/// </param>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    IReadOnlyDictionary<string, string>? Parameters,
    bool IsRead,
    DateTime CreatedAt);
