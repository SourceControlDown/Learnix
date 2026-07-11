using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

/// <summary>
/// The goodbye email. It is the only place the user learns that the deletion is reversible, so it names the
/// day their data is erased on and tells them who can undo it before then.
/// </summary>
internal sealed class UserDeletedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserDeletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        // The soft-delete filter would hide the very user this email is about.
        var user = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == e.UserId)
            .Select(u => new { u.Email, u.FirstName, u.Language })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return;

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.AccountDeletedEmail,
            new SendAccountDeletedEmailPayload(
                user.Email!,
                user.FirstName,
                e.PurgeAfterUtc,
                user.Language)));
    }
}
