using Learnix.Domain.Events.Category;
using Learnix.Infrastructure.Outbox;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Categories;

internal sealed class CategoryImageRemovedOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<CategoryImageRemovedDomainEvent, DeleteBlobPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.DeleteBlob;
    protected override DeleteBlobPayload BuildPayload(CategoryImageRemovedDomainEvent e)
        => new(e.ImageBlobPath);
}
