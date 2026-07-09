using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Specifications;

public sealed class PublishedCoursesByInstructorSpecification : Specification<Course>
{
    public PublishedCoursesByInstructorSpecification(Guid instructorId, int take)
    {
        Query
            .Where(c => c.InstructorId == instructorId && c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.EnrollmentsCount)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(take)
            .AsNoTracking();
    }
}
