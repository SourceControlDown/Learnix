using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByIdForUpdateSpecification : Specification<Course>
{
    public CourseByIdForUpdateSpecification(Guid id)
    {
        Criteria = c => c.Id == id;
        AsNoTracking = false;
    }
}
