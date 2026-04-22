using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Abstractions;

public interface ILessonRepository : IRepositoryBase<Lesson>
{
    Task<int> GetMaxDisplayOrderAsync(Guid sectionId, CancellationToken ct = default);
    Task ShiftLessonsOrderAsync(Guid sectionId, Guid lessonId, int newOrder, CancellationToken ct);
    Task<T?> GetLessonOfTypeByIdAsync<T>(Guid id, bool forUpdate = false, CancellationToken ct = default) where T : Lesson;
}
