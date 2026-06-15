using Learnix.Domain.Events.Course;
using Learnix.Infrastructure.Outbox;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Courses;

internal sealed class CourseCoverSetOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<CourseCoverSetDomainEvent, MarkBlobConfirmedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.MarkBlobConfirmed;
    protected override MarkBlobConfirmedPayload BuildPayload(CourseCoverSetDomainEvent e)
        => new(e.CoverBlobPath);
}
