using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Lessons;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Persistence;
using MediatR;
using System.Text.Json;

internal sealed class LessonVideoReleasedHandler(ApplicationDbContext db)
    : INotificationHandler<DomainEventNotification<LessonVideoReleasedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LessonVideoReleasedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.DeleteBlob,
            Payload = JsonSerializer.Serialize(new { e.ReleasedBlobPath }),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}