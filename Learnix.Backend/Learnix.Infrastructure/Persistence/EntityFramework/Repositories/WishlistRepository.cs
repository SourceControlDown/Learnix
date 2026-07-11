using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class WishlistRepository(ApplicationDbContext context) : IWishlistRepository
{
    public Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
        => context.WishlistItems.AnyAsync(w => w.UserId == userId && w.CourseId == courseId, cancellationToken);

    public async Task AddIfMissingAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var exists = await ExistsAsync(userId, courseId, cancellationToken);
        if (!exists)
            await context.WishlistItems.AddAsync(WishlistItem.Create(userId, courseId), cancellationToken);
    }

    public async Task RemoveIfExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.CourseId == courseId, cancellationToken);

        if (item is not null)
            context.WishlistItems.Remove(item);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken)
        => context.WishlistItems.CountAsync(w => w.UserId == userId, cancellationToken);

    public Task<List<WishlistItem>> GetPagedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken)
        => context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.Course)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
}
