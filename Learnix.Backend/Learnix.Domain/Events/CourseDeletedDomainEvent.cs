using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

// Phase 7+ consumer: notify enrolled students via email that course was removed.
public sealed record CourseDeletedDomainEvent(Guid CourseId) : IDomainEvent;