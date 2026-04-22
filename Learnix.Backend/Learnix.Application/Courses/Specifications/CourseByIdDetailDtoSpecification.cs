using Ardalis.Specification;
using Learnix.Application.Courses.Queries.GetCourseById;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseByIdDetailDtoSpecification : Specification<Course, CourseDetailDto>, ISingleResultSpecification<Course, CourseDetailDto>
{
    public CourseByIdDetailDtoSpecification(Guid id)
    {
        Query.Where(c => c.Id == id)
            .Select(c => new CourseDetailDto(
                c.Id,
                c.InstructorId,
                c.CategoryId,
                c.Title,
                c.Description,
                c.CoverImageUrl,
                c.Price,
                c.Price == 0m,
                c.Status.ToString(),
                c.EnrollmentsCount,
                c.Tags.ToList(),
                c.Sections
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new SectionDto(
                        s.Id,
                        s.Title,
                        s.DisplayOrder,
                        s.Lessons
                            .OrderBy(l => l.DisplayOrder)
                            .Select(l => new LessonSummaryDto(l.Id, l.Title, l.DisplayOrder, l.LessonType.ToString()))
                            .ToList()))
                    .ToList(),
                c.CreatedAt,
                c.UpdatedAt));
    }
}