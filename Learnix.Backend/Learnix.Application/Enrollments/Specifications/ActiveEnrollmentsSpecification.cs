using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.Enrollments.Specifications;

/// <summary>
/// The student's courses still in progress, newest first. An unpaid enrollment grants no access,
/// and a completed one is nothing left to continue, so both are excluded.
/// </summary>
public sealed class ActiveEnrollmentsSpecification : Specification<Enrollment>
{
    public ActiveEnrollmentsSpecification(Guid studentId)
    {
        Query
            .Where(e => e.StudentId == studentId
                        && e.Status == EnrollmentStatus.Active
                        && e.PaymentStatus == PaymentStatus.Completed)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .AsNoTracking();
    }
}
