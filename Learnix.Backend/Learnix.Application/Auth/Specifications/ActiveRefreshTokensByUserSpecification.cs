using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Auth.Specifications;

public sealed class ActiveRefreshTokensByUserSpecification : Specification<RefreshToken>
{
    public ActiveRefreshTokensByUserSpecification(Guid userId)
    {
        var now = DateTime.UtcNow;
        Criteria = x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > now;
        AsNoTracking = false;
    }
}
