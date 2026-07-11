using Learnix.Domain.Common;

namespace Learnix.Domain.Events.User;

/// <summary>An admin brought a deleted account back within the recovery window.</summary>
public sealed record UserRecoveredDomainEvent(Guid UserId) : DomainEvent;
