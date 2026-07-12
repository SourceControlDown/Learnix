using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.AiChat.Specifications;

public sealed class UsersByIdsSpecification : Specification<User>
{
    public UsersByIdsSpecification(IReadOnlyList<Guid> ids)
    {
        Query.Where(u => ids.Contains(u.Id)).AsNoTracking();
    }
}
