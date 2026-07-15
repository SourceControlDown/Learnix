using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Reviews.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;

public sealed class GetInstructorRatingDistributionQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICourseReviewRepository reviewRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorRatingDistributionQuery, InstructorRatingDistributionDto>(currentUser)
{
    protected override async Task<Result<InstructorRatingDistributionDto>> HandleAsync(
        GetInstructorRatingDistributionQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        if (courses.Count == 0)
            return Result.Ok(new InstructorRatingDistributionDto(0, 0, 0, 0, 0));

        var courseIds = courses.Select(c => c.Id).ToList();

        var reviews = await reviewRepository.ListAsync(
            new InstructorReviewsSpecification(courseIds),
            cancellationToken);

        var oneStar = reviews.Count(r => r.Rating == 1);
        var twoStar = reviews.Count(r => r.Rating == 2);
        var threeStar = reviews.Count(r => r.Rating == 3);
        var fourStar = reviews.Count(r => r.Rating == 4);
        var fiveStar = reviews.Count(r => r.Rating == 5);

        return Result.Ok(new InstructorRatingDistributionDto(oneStar, twoStar, threeStar, fourStar, fiveStar));
    }
}
