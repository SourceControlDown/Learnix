using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Lessons;

public sealed record LessonVideoSetDomainEvent(
    Guid LessonId,
    string AttachedBlobPath
) : DomainEvent;
