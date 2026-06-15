using Learnix.Domain.Events.Course;
using Learnix.Infrastructure.Outbox;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Courses;

internal sealed class CourseCoverRemovedOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<CourseCoverRemovedDomainEvent, DeleteBlobPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.DeleteBlob;
    protected override DeleteBlobPayload BuildPayload(CourseCoverRemovedDomainEvent e)
        => new(e.CoverBlobPath);
}
