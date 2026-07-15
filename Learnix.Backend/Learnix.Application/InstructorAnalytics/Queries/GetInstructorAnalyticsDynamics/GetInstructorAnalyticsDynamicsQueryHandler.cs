using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

public sealed class GetInstructorAnalyticsDynamicsQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    IPaymentRepository paymentRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorAnalyticsDynamicsQuery, List<InstructorAnalyticsDynamicsItemDto>>(currentUser)
{
    protected override async Task<Result<List<InstructorAnalyticsDynamicsItemDto>>> HandleAsync(
        GetInstructorAnalyticsDynamicsQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var enrollments = await enrollmentRepository.ListAsync(
            new InstructorEnrollmentsByDateSpecification(instructorId, request.StartDate, request.EndDate),
            cancellationToken);

        var payments = await paymentRepository.ListAsync(
            new InstructorPaymentsByDateSpecification(instructorId, request.StartDate, request.EndDate),
            cancellationToken);

        // Group by day
        var enrollmentGroups = enrollments
            .GroupBy(e => e.EnrolledAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var paymentGroups = payments
            .GroupBy(p => p.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        // Create a continuous list of dates from StartDate to EndDate
        var result = new List<InstructorAnalyticsDynamicsItemDto>();
        for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
        {
            var dailyEnrollments = enrollmentGroups.GetValueOrDefault(date, 0);
            var dailyEarnings = paymentGroups.GetValueOrDefault(date, 0m);

            result.Add(new InstructorAnalyticsDynamicsItemDto(
                date.ToString("yyyy-MM-dd"),
                dailyEnrollments,
                dailyEarnings));
        }

        return Result.Ok(result);
    }
}
