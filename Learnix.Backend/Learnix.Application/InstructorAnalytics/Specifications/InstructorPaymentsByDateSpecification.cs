using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.InstructorAnalytics.Specifications;

public sealed class InstructorPaymentsByDateSpecification : Specification<Payment>
{
    public InstructorPaymentsByDateSpecification(Guid instructorId, DateTime startDate, DateTime endDate)
    {
        Query.Where(p => p.Status == PaymentStatus.Completed &&
                         p.Course != null &&
                         p.Course.InstructorId == instructorId &&
                         p.CreatedAt >= startDate &&
                         p.CreatedAt <= endDate);
        Query.AsNoTracking();
    }
}
