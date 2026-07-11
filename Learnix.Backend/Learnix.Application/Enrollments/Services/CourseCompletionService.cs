using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.Enrollments.Services;

internal sealed class CourseCompletionService(
    ILessonRepository lessonRepository,
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository,
    ICertificateRepository certificateRepository)
    : ICourseCompletionService
{
    public async Task TryCompleteAsync(
        Guid studentId,
        Guid courseId,
        Guid? justCompletedLessonId,
        CancellationToken ct = default)
    {
        var lessons = await lessonRepository.GetVisibleLessonCompletionAsync(studentId, courseId, ct);

        // A course with nothing to learn cannot be finished.
        if (lessons.Count == 0)
            return;

        var allDone = lessons.All(l => l.IsCompleted || l.LessonId == justCompletedLessonId);

        if (!allDone)
            return;

        var enrollment = await enrollmentRepository.FirstOrDefaultAsync(
            new EnrollmentByStudentAndCourseSpecification(studentId, courseId, forUpdate: true), ct);

        if (enrollment is null || enrollment.Status == EnrollmentStatus.Completed)
            return;

        var course = await courseRepository.GetByIdAsync(courseId, ct);

        if (course is null)
            return;

        enrollment.MarkCompleted();

        certificateRepository.Add(Certificate.Issue(enrollment, course));
    }
}
