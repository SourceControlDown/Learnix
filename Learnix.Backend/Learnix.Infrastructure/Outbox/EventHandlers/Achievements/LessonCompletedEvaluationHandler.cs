using Learnix.Application.Common.Events;
using Learnix.Domain.Events.LessonProgress;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class LessonCompletedEvaluationHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<LessonCompletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LessonCompletedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.EvaluateLessonCompleted,
            Payload = JsonSerializer.Serialize(new EvaluateLessonCompletedPayload(e.StudentId)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
