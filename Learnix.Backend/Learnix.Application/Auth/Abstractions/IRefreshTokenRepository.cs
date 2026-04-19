using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Auth.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FirstOrDefaultAsync(Specification<RefreshToken> spec, CancellationToken ct = default);
    Task<List<RefreshToken>> ListAsync(Specification<RefreshToken> spec, CancellationToken ct = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);
}