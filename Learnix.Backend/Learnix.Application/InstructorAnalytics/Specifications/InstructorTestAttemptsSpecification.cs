using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorTestAttemptsSpecification : Specification<TestAttempt>
{
    public InstructorTestAttemptsSpecification(List<Guid> courseIds)
    {
        Query.Where(t => courseIds.Contains(t.CourseId) && t.SubmittedAt.HasValue);
        Query.AsNoTracking();
    }
}
