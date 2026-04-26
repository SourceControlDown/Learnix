using FluentResults;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseById;

public sealed class GetCourseByIdQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCourseByIdQuery, Result<CourseDetailDto>>
{
    public async Task<Result<CourseDetailDto>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, includeLessons: true),
            cancellationToken);

        if (course is null || course.Status != CourseStatus.Published)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        var dto = new CourseDetailDto(
            course.Id,
            course.InstructorId,
            course.CategoryId,
            course.Title,
            course.Description,
            course.CoverBlobPath,
            course.Price,
            course.Price == 0m,
            course.EnrollmentsCount,
            course.Tags.ToList(),
            course.Sections
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new SectionDto(
                    s.Id,
                    s.Title,
                    s.DisplayOrder,
                    s.Lessons
                        .Where(l => !l.IsHidden)
                        .OrderBy(l => l.DisplayOrder)
                        .Select(l => new LessonSummaryDto(
                            l.Id,
                            l.Title,
                            l.DisplayOrder,
                            l.LessonType.ToString()))
                        .ToList()))
                .ToList(),
            course.CreatedAt,
            course.UpdatedAt);

        return Result.Ok(dto);
    }
}
