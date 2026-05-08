using Learnix.Domain.Events.Lessons;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Lessons;

internal sealed class LessonVideoAttachedHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<LessonVideoAttachedDomainEvent, MarkBlobConfirmedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.MarkBlobConfirmed;
    protected override MarkBlobConfirmedPayload BuildPayload(LessonVideoAttachedDomainEvent e)
        => new(e.AttachedBlobPath);
}
