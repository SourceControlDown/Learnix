using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByIdSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public CourseByIdSpecification(
        Guid id,
        bool includeSections = false,
        bool includeLessons = false,
        bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (includeSections)
        {
            Query.Include(c => c.Sections);

            if (includeLessons)
            {
                Query.Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons);
            }
        }

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}
