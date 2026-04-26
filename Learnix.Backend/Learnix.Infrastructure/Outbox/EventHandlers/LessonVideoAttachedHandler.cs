using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Lessons;
using Learnix.Infrastructure.Persistence;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers;

internal sealed class LessonVideoAttachedHandler(ApplicationDbContext db)
    : INotificationHandler<DomainEventNotification<LessonVideoAttachedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LessonVideoAttachedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.MarkBlobConfirmed,
            Payload = JsonSerializer.Serialize(new { e.AttachedBlobPath }),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
