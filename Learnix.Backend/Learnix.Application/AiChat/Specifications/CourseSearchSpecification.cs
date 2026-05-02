using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Specifications;

public sealed class CourseSearchSpecification : Specification<Course>
{
    public CourseSearchSpecification(string query, string? categorySlug, int maxResults)
    {
        Query.Where(c => c.Status == CourseStatus.Published);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLower();
            Query.Where(c => c.Title.ToLower().Contains(normalized)
                           || c.Description.ToLower().Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(categorySlug))
            Query.Where(c => c.Category!.Slug == categorySlug);

        Query
            .Include(c => c.Category)
            .OrderByDescending(c => c.EnrollmentsCount)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(maxResults)
            .AsNoTracking();
    }
}
