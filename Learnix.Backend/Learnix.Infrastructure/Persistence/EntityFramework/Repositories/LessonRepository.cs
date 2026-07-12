using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class LessonRepository(ApplicationDbContext context)
    : RepositoryBase<Lesson>(context), ILessonRepository
{
    public Task<T?> GetLessonOfTypeByIdAsync<T>(Guid id, bool forUpdate = false, CancellationToken cancellationToken = default)
            where T : Lesson
    {
        var query = context.Set<Lesson>().OfType<T>();

        if (!forUpdate)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public Task<bool> IsLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        return (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lesson.Id == lessonId && section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).AnyAsync(cancellationToken);
    }

    public Task<TestLesson?> GetTestLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        return (
            from lesson in context.Set<TestLesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lesson.Id == lessonId && section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LessonCompletion>> GetVisibleLessonCompletionAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where section.CourseId == courseId && !lesson.IsHidden
            select new LessonCompletion(
                lesson.Id,
                context.Set<LessonProgress>()
                    .Any(lp => lp.LessonId == lesson.Id && lp.StudentId == studentId && lp.IsCompleted))
        ).AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<Lesson?> GetVisibleLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        return (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lesson.Id == lessonId && section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetResumeLessonIdAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var lessons = await (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where section.CourseId == courseId && !lesson.IsHidden
            orderby section.DisplayOrder, lesson.DisplayOrder
            select new
            {
                lesson.Id,
                IsCompleted = context.Set<LessonProgress>()
                    .Any(lp => lp.LessonId == lesson.Id && lp.StudentId == studentId && lp.IsCompleted)
            }
        ).AsNoTracking().ToListAsync(cancellationToken);

        if (lessons.Count == 0)
            return null;

        var next = lessons.FirstOrDefault(l => !l.IsCompleted);

        return next?.Id ?? lessons[0].Id;
    }
}
