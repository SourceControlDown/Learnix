using Learnix.Application.Achievements.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class UserAchievementProgressRepository(ApplicationDbContext context)
    : IUserAchievementProgressRepository
{
    public Task<UserAchievementProgress?> GetAsync(Guid userId, CancellationToken cancellationToken)
        => context.UserAchievementProgresses.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<UserAchievementProgress> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await GetAsync(userId, cancellationToken);
        if (existing is not null)
            return existing;

        var created = UserAchievementProgress.Create(userId);
        await context.UserAchievementProgresses.AddAsync(created, cancellationToken);
        return created;
    }
}
