using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Domain.Enums;

namespace Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;

public sealed class GetCourseStatusesQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository)
    : InstructorAnalyticsQueryHandler<GetCourseStatusesQuery, CourseStatusesDto>(currentUser)
{
    protected override async Task<Result<CourseStatusesDto>> HandleAsync(
        GetCourseStatusesQuery request, Guid instructorId, CancellationToken cancellationToken)
    {

        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        var draft = courses.Count(c => c.Status == CourseStatus.Draft);
        var published = courses.Count(c => c.Status == CourseStatus.Published);
        var archived = courses.Count(c => c.Status == CourseStatus.Archived);

        return Result.Ok(new CourseStatusesDto(draft, published, archived));
    }
}
