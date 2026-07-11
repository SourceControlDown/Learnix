using Learnix.Application.Courses.Abstractions;

namespace Learnix.Application.Categories.Services;

internal sealed class CategoryCoursesCountUpdater(ICategoryRepository categories)
{
    public async Task IncrementAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(categoryId, cancellationToken);
        category?.IncrementCoursesCount();
    }

    public async Task DecrementAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(categoryId, cancellationToken);
        category?.DecrementCoursesCount();
    }
}
