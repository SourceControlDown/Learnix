using Learnix.Application.Common.Events;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

/// <summary>
/// Avatar changes feed into the same profile-completeness check as <see cref="UserProfileUpdatedDomainEvent"/>.
/// </summary>
internal sealed class UserAvatarSetEvaluationHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserAvatarSetDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<UserAvatarSetDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.EvaluateProfileChanged,
            Payload = JsonSerializer.Serialize(new EvaluateProfileChangedPayload(e.UserId)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
