using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.AiChat.Specifications;

/// <summary>
/// Users whose first name, last name, or full name contains the search term. Role membership lives in the
/// Identity tables and is not reachable from a <see cref="Specification{T}"/> — the caller narrows the
/// result down to instructors afterwards.
/// </summary>
public sealed class InstructorCandidatesByNameSpecification : Specification<User>
{
    public InstructorCandidatesByNameSpecification(string name, int take)
    {
        var normalized = name.Trim().ToLower();

        Query
            .Where(u => u.FirstName.ToLower().Contains(normalized)
                     || u.LastName.ToLower().Contains(normalized)
                     || (u.FirstName + " " + u.LastName).ToLower().Contains(normalized))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(take)
            .AsNoTracking();
    }
}
