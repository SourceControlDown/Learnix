namespace Learnix.Application.Enrollments.Abstractions;

/// <summary>
/// The one place that decides a course is finished and hands out the certificate for it.
/// </summary>
public interface ICourseCompletionService
{
    /// <summary>
    /// Completes the enrollment and issues a certificate when every visible lesson of the course is
    /// done. Stages the changes only — the caller commits them, so this runs inside the same
    /// transaction as whatever completed the lesson.
    /// <para>
    /// Does nothing when lessons remain, when the course has no visible lessons, or when the
    /// enrollment is already complete — so it is safe to call more than once.
    /// </para>
    /// </summary>
    /// <param name="justCompletedLessonId">
    /// The lesson being completed in this very transaction. It counts as done even though its
    /// progress row may not have been flushed yet. Pass <c>null</c> to judge purely on what is
    /// already in the database.
    /// </param>
    Task TryCompleteAsync(
        Guid studentId,
        Guid courseId,
        Guid? justCompletedLessonId,
        CancellationToken cancellationToken = default);
}
