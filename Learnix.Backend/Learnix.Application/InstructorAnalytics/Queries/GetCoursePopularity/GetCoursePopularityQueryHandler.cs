using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;

namespace Learnix.Application.InstructorAnalytics.Queries.GetCoursePopularity;

public sealed class GetCoursePopularityQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository)
    : InstructorAnalyticsQueryHandler<GetCoursePopularityQuery, List<CoursePopularityItemDto>>(currentUser)
{
    protected override async Task<Result<List<CoursePopularityItemDto>>> HandleAsync(
        GetCoursePopularityQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        var result = courses
            .OrderByDescending(c => c.EnrollmentsCount)
            .Select(c => new CoursePopularityItemDto(c.Id, c.Title, c.EnrollmentsCount))
            .ToList();

        return Result.Ok(result);
    }
}
