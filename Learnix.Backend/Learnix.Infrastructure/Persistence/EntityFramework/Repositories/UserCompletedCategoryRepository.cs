using Learnix.Application.Achievements.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class UserCompletedCategoryRepository(ApplicationDbContext context)
    : IUserCompletedCategoryRepository
{
    public async Task<bool> AddIfMissingAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        var exists = await context.UserCompletedCategories
            .AnyAsync(uc => uc.UserId == userId && uc.CategoryId == categoryId, cancellationToken);

        if (exists) return false;

        await context.UserCompletedCategories.AddAsync(
            UserCompletedCategory.Create(userId, categoryId), cancellationToken);
        return true;
    }

    public Task<int> CountDistinctCategoriesAsync(Guid userId, CancellationToken cancellationToken)
        => context.UserCompletedCategories.CountAsync(uc => uc.UserId == userId, cancellationToken);
}
