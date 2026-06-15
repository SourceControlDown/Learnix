using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads;
using Learnix.Infrastructure.Outbox;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserAvatarSetOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<UserAvatarSetDomainEvent, MarkBlobConfirmedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.MarkBlobConfirmed;
    protected override MarkBlobConfirmedPayload BuildPayload(UserAvatarSetDomainEvent e)
        => new(e.BlobPath);
}
