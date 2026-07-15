using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Reviews.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRecentReviews;

public sealed class GetInstructorRecentReviewsQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorRecentReviewsQuery, List<InstructorRecentReviewDto>>(currentUser)
{
    protected override async Task<Result<List<InstructorRecentReviewDto>>> HandleAsync(
        GetInstructorRecentReviewsQuery request, Guid instructorId, CancellationToken cancellationToken)
    {

        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        if (courses.Count == 0)
            return Result.Ok(new List<InstructorRecentReviewDto>());

        var courseIds = courses.Select(c => c.Id).ToList();

        var reviews = await reviewRepository.ListAsync(
            new InstructorReviewsSpecification(courseIds),
            cancellationToken);

        var result = reviews
            .Take(request.Take)
            .Select(r => new InstructorRecentReviewDto(
                r.CourseId,
                courses.First(c => c.Id == r.CourseId).Title,
                r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : "Unknown Student",
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToList();

        return Result.Ok(result);
    }
}
