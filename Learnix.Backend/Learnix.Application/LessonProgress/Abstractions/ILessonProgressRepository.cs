using Ardalis.Specification;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Abstractions;

public interface ILessonProgressRepository : IRepositoryBase<LessonProgressEntity>
{
    /// <summary>
    /// Returns completed and total visible lesson counts for the student in each of the given courses.
    /// Every requested course id is present in the result, even when the course has no visible lessons.
    /// </summary>
    /// <remarks>
    /// Visibility lives on <c>Lesson.IsHidden</c>, and <c>LessonProgress</c> has no navigation to <c>Lesson</c>,
    /// so this cannot be expressed as a <see cref="Specification{T}"/> and is a custom method instead.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, CourseProgressCounts>> GetProgressCountsAsync(
        Guid studentId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken ct = default);
}
