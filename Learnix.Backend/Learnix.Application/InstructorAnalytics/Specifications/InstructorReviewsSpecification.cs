using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorReviewsSpecification : Specification<CourseReview>
{
    public InstructorReviewsSpecification(List<Guid> courseIds)
    {
        Query.Where(r => courseIds.Contains(r.CourseId));
        Query.Include(r => r.Student);
        Query.OrderByDescending(r => r.CreatedAt);
        Query.AsNoTracking();
    }
}
