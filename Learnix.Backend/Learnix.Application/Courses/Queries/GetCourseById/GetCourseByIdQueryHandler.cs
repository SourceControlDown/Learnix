using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseById;

public sealed class GetCourseByIdQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository)
    : IRequestHandler<GetCourseByIdQuery, Result<CourseDetailDto>>
{
    public async Task<Result<CourseDetailDto>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdWithStructureSpecification(request.CourseId),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        var isOwner = currentUser.UserId is not null && course.InstructorId == currentUser.UserId.Value;
        var isAdmin = currentUser.IsInRole(Roles.Admin);

        // Non-owners / non-admins can only see Published courses.
        // Return NotFound (not Forbidden) — do not leak existence of draft/archived content.
        if (!isOwner && !isAdmin && course.Status != CourseStatus.Published)
            return Result.Fail(new NotFoundError($"Course '{request.CourseId}' was not found."));

        return Result.Ok(Map(course));
    }

    private static CourseDetailDto Map(Course course) => new(
        Id: course.Id,
        InstructorId: course.InstructorId,
        CategoryId: course.CategoryId,
        Title: course.Title,
        Description: course.Description,
        CoverImageUrl: course.CoverImageUrl,
        Price: course.Price,
        IsFree: course.Price == 0m,
        Status: course.Status.ToString(),
        EnrollmentsCount: course.EnrollmentsCount,
        Tags: course.Tags.ToList(),
        Sections: course.Sections
            .OrderBy(s => s.Order)
            .Select(s => new SectionDto(
                s.Id,
                s.Title,
                s.Order,
                s.Lessons
                    .OrderBy(l => l.Order)
                    .Select(l => new LessonSummaryDto(l.Id, l.Title, l.Order, l.LessonType.ToString()))
                    .ToList()))
            .ToList(),
        CreatedAt: course.CreatedAt,
        UpdatedAt: course.UpdatedAt);
}