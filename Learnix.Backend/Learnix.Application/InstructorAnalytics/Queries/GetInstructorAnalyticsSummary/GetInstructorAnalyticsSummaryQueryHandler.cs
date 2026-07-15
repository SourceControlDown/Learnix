using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Specifications;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;

public sealed class GetInstructorAnalyticsSummaryQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IPaymentRepository paymentRepository,
    ICertificateRepository certificateRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorAnalyticsSummaryQuery, InstructorAnalyticsSummaryDto>(currentUser)
{
    protected override async Task<Result<InstructorAnalyticsSummaryDto>> HandleAsync(
        GetInstructorAnalyticsSummaryQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(new InstructorCoursesForAnalyticsSpecification(instructorId), cancellationToken);
        var payments = await paymentRepository.ListAsync(new InstructorPaymentsSpecification(instructorId), cancellationToken);
        var certificates = await certificateRepository.CountAsync(new InstructorCertificatesSpecification(instructorId), cancellationToken);

        var totalStudents = courses.Sum(c => c.EnrollmentsCount);
        var totalEarnings = payments.Sum(p => p.Amount);

        var coursesWithReviews = courses.Where(c => c.ReviewsCount > 0).ToList();
        var averageRating = coursesWithReviews.Count > 0
            ? (double)coursesWithReviews.Average(c => c.AverageRating)
            : 0;

        return Result.Ok(new InstructorAnalyticsSummaryDto(
            totalStudents,
            totalEarnings,
            Math.Round(averageRating, 2),
            certificates));
    }
}
