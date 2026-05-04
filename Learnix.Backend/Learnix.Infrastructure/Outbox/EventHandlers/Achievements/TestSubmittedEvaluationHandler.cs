using Learnix.Application.Common.Events;
using Learnix.Domain.Events.TestAttempts;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class TestSubmittedEvaluationHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<TestSubmittedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TestSubmittedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.EvaluateTestSubmitted,
            Payload = JsonSerializer.Serialize(new EvaluateTestSubmittedPayload(
                e.StudentId, e.QuestionsCount, e.DurationSeconds, e.Passed)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
