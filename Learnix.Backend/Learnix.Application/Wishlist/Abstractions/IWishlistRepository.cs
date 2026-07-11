using Learnix.Domain.Entities;

namespace Learnix.Application.Wishlist.Abstractions;

public interface IWishlistRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task AddIfMissingAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task RemoveIfExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<WishlistItem>> GetPagedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken);
}
