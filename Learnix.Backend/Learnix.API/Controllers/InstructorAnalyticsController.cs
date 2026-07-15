using Learnix.API.Extensions;
using Learnix.Application.InstructorAnalytics.Queries.GetCoursePopularity;
using Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRecentReviews;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorTestPerformance;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/instructor/analytics")]
[Authorize(Roles = Roles.Instructor)]
public sealed class InstructorAnalyticsController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorAnalyticsSummaryQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("dynamics")]
    public async Task<IActionResult> GetDynamics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorAnalyticsDynamicsQuery(startDate, endDate), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("courses/popularity")]
    public async Task<IActionResult> GetCoursePopularity(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCoursePopularityQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("courses/statuses")]
    public async Task<IActionResult> GetCourseStatuses(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseStatusesQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("reviews/distribution")]
    public async Task<IActionResult> GetRatingDistribution(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorRatingDistributionQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("reviews/recent")]
    public async Task<IActionResult> GetRecentReviews([FromQuery] int take = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInstructorRecentReviewsQuery(take), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("tests/performance")]
    public async Task<IActionResult> GetTestPerformance(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorTestPerformanceQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }
}
