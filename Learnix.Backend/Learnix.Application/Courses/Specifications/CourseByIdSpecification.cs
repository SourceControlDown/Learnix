using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByIdSpecification : Specification<Course>
{
    public CourseByIdSpecification(Guid id)
    {
        Criteria = c => c.Id == id;
    }
}
