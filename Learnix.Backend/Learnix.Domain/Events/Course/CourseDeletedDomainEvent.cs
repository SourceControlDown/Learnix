using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseDeletedDomainEvent(Guid CourseId) : DomainEvent;
