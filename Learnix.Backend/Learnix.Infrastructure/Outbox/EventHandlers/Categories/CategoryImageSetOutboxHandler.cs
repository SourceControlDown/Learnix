using Learnix.Domain.Events.Category;
using Learnix.Infrastructure.Outbox;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Categories;

internal sealed class CategoryImageSetOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<CategoryImageSetDomainEvent, MarkBlobConfirmedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.MarkBlobConfirmed;
    protected override MarkBlobConfirmedPayload BuildPayload(CategoryImageSetDomainEvent e)
        => new(e.ImageBlobPath);
}
