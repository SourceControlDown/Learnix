using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorEnrollmentsByDateSpecification : Specification<Enrollment>
{
    public InstructorEnrollmentsByDateSpecification(Guid instructorId, DateTime startDate, DateTime endDate)
    {
        Query.Where(e => e.Course != null &&
                         e.Course.InstructorId == instructorId &&
                         e.EnrolledAt >= startDate &&
                         e.EnrolledAt <= endDate);
        Query.AsNoTracking();
    }
}
