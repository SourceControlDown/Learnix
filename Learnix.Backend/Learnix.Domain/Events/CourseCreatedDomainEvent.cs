using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

// Consumers come online in Phase 7+ (notifications, achievements).
public sealed record CourseCreatedDomainEvent(
    Guid CourseId,
    Guid InstructorId,
    Guid CategoryId) : IDomainEvent;
