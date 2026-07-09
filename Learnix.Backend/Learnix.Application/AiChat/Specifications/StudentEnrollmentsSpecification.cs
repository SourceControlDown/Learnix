using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Specifications;

/// <summary>
/// Every enrollment the student actually has access to, newest first. Enrollments whose payment has not
/// completed grant no access, so they are not part of the student's learning picture.
/// </summary>
public sealed class StudentEnrollmentsSpecification : Specification<Enrollment>
{
    public StudentEnrollmentsSpecification(Guid studentId)
    {
        Query
            .Where(e => e.StudentId == studentId && e.PaymentStatus == PaymentStatus.Completed)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .AsNoTracking();
    }
}
