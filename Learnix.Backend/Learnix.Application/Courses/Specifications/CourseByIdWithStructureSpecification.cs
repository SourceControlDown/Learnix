using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

/// <summary>
/// Loads course + sections + lessons. Used by:
/// - Publish command (invariant checks require full structure)
/// - GetCourseById query
/// </summary>
public sealed class CourseByIdWithStructureSpecification : Specification<Course>
{
    public CourseByIdWithStructureSpecification(Guid id, bool forUpdate = false)
    {
        Criteria = c => c.Id == id;

        // Nested include via string — `Sections.Lessons`.
        AddInclude($"{nameof(Course.Sections)}.{nameof(Section.Lessons)}");

        AsNoTracking = !forUpdate;
    }
}