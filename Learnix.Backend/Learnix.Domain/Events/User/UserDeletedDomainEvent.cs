using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

/// <summary>
/// The account was soft-deleted. The row survives, so the handler that reacts to this must read the user
/// with <c>IgnoreQueryFilters</c> — by the time it runs, the global filter would hide them.
/// </summary>
/// <param name="PurgeAfterUtc">
/// When the personal data is erased for good. Carried here rather than read back from the row: events are
/// dispatched before the UPDATE, so the database still says null.
/// </param>
public sealed record UserDeletedDomainEvent(Guid UserId, DateTime PurgeAfterUtc) : DomainEvent;
