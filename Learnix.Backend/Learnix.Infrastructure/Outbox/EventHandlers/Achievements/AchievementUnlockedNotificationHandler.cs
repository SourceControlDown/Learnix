using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Achievements;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

/// <summary>
/// When an achievement is unlocked, enqueue a notification message.
/// Phase 2 will dispatch this to SignalR; for now the message is recorded
/// and the dispatcher (no-op) leaves it processed.
/// </summary>
internal sealed class AchievementUnlockedNotificationHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<AchievementUnlockedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<AchievementUnlockedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.NotifyAchievementUnlocked,
            Payload = JsonSerializer.Serialize(new NotifyAchievementUnlockedPayload(
                e.UserAchievementId, e.UserId, e.Code, e.UnlockedAt)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
