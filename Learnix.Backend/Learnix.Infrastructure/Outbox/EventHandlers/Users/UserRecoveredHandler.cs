using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Users;

internal sealed class UserRecoveredHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<UserRecoveredDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRecoveredDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        // Domain events are dispatched before the UPDATE runs (ADR-BACK-ARCH-008), so the row is still
        // flagged deleted in the database: the filter has to be off here too.
        var user = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == e.UserId)
            .Select(u => new { u.Email, u.FirstName, u.Language })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        db.OutboxMessages.Add(OutboxMessage.Create(
            e.EventId,
            OutboxMessageTypes.AccountRecoveredEmail,
            new SendAccountRecoveredEmailPayload(user.Email!, user.FirstName, user.Language)));
    }
}
