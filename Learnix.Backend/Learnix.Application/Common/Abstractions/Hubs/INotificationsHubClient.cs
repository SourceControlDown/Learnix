namespace Learnix.Application.Common.Abstractions.Hubs;

public interface INotificationsHubClient
{
    Task ReceiveMessage(NewMessageNotification notification);
    Task UnreadCountChanged(UnreadCountNotification notification);
    Task AchievementUnlocked(AchievementUnlockedNotification notification);
    Task CertificateIssued(CertificateIssuedNotification notification);
    Task NotificationReceived(NotificationReceivedPayload notification);
}

public sealed record NewMessageNotification(
    Guid ConversationId,
    Guid MessageId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatarPath,
    string Content,
    DateTime SentAt);

public sealed record UnreadCountNotification(int TotalUnread);

public sealed record AchievementUnlockedNotification(
    Guid AchievementId,
    string Code,
    DateTime UnlockedAt);

/// <summary>
/// <paramref name="CourseId"/> is sent for the client to key off later; nothing consumes it yet.
/// </summary>
public sealed record CertificateIssuedNotification(
    Guid CertificateId,
    Guid CourseId,
    string CourseTitle);

/// <param name="Parameters">
/// What the client cannot derive from the type — a course title, an achievement code. Null when the type says
/// it all. The wording is the client's to choose; the server only reports what happened (ADR-NOTIF-001).
/// </param>
public sealed record NotificationReceivedPayload(
    Guid NotificationId,
    string Type,
    IReadOnlyDictionary<string, string>? Parameters,
    DateTime CreatedAt);
