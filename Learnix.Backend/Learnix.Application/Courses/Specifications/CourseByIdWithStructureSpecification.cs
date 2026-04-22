using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByIdWithStructureSpecification : Specification<Course>, ISingleResultSpecification<Course>
{
    public CourseByIdWithStructureSpecification(Guid id, bool forUpdate = false)
    {
        Query
            .Where(c => c.Id == id)
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons);

        if (!forUpdate)
        {
            Query.AsNoTracking();
        }
    }
}