using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Enrollments;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using MediatR;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Achievements;

internal sealed class EnrollmentCompletedEvaluationHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<EnrollmentCompletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<EnrollmentCompletedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.EvaluateEnrollmentCompleted,
            Payload = JsonSerializer.Serialize(new EvaluateEnrollmentCompletedPayload(e.StudentId, e.CourseId)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
