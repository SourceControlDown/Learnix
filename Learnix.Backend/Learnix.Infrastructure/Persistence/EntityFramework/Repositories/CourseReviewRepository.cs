using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class CourseReviewRepository(ApplicationDbContext context)
    : RepositoryBase<CourseReview>(context), ICourseReviewRepository
{
    public async Task<(int Count, decimal Average)> GetCourseRatingMetricsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var stats = await context.CourseReviews
            .Where(r => r.CourseId == courseId)
            .GroupBy(r => r.CourseId)
            .Select(g => new { Count = g.Count(), Average = g.Average(r => (decimal)r.Rating) })
            .FirstOrDefaultAsync(cancellationToken);

        return stats == null ? (0, 0m) : (stats.Count, Math.Round(stats.Average, 2));
    }
}
