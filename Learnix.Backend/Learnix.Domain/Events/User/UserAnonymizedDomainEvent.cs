using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

/// <summary>
/// The recovery window ran out and the account's personal data was erased. There is nobody left to email —
/// the address is gone — so this exists for the audit trail and for anything that has to forget the user too.
/// </summary>
public sealed record UserAnonymizedDomainEvent(Guid UserId) : DomainEvent;
