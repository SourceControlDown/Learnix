using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;

namespace Learnix.Application.Auth.Specifications;

public sealed class RefreshTokenByHashSpecification : Specification<RefreshToken>
{
    public RefreshTokenByHashSpecification(string tokenHash)
    {
        Criteria = x => x.TokenHash == tokenHash;
        AsNoTracking = false; // we mutate on find (revoke)
    }
}