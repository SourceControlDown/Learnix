using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorCertificatesSpecification : Specification<Certificate>
{
    public InstructorCertificatesSpecification(Guid instructorId)
    {
        Query.Where(c => c.Course != null && c.Course.InstructorId == instructorId);
        Query.AsNoTracking();
    }
}
