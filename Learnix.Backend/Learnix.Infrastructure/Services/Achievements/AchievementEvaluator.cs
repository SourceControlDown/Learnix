using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services.Achievements;

/// <summary>
/// Each method recomputes from authoritative aggregates rather than incrementing,
/// so re-processing the same outbox message is a safe no-op (counter idempotency).
/// Achievement insertion is guarded by <see cref="IUserAchievementRepository.HasAchievementAsync"/>.
/// </summary>
internal sealed class AchievementEvaluator(
    ApplicationDbContext db,
    IUserAchievementRepository achievementRepo,
    IUserAchievementProgressRepository progressRepo,
    IUserCompletedCategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IAchievementEvaluator
{
    private static readonly (int Threshold, string Code)[] LessonThresholds =
    [
        (1, AchievementCodes.FirstLesson),
        (50, AchievementCodes.Lessons50),
        (200, AchievementCodes.Lessons200),
        (500, AchievementCodes.Lessons500),
    ];

    private static readonly (int Threshold, string Code)[] CourseThresholds =
    [
        (1, AchievementCodes.FirstCourse),
        (3, AchievementCodes.Courses3),
        (5, AchievementCodes.Courses5),
    ];

    public async Task OnLessonCompletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var lessonsCompleted = await db.LessonProgresses
            .CountAsync(lp => lp.StudentId == userId && lp.IsCompleted, cancellationToken);

        var progress = await progressRepo.GetOrCreateAsync(userId, cancellationToken);
        progress.SetLessonsCompleted(lessonsCompleted);

        await UnlockThresholdsAsync(userId, lessonsCompleted, LessonThresholds, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task OnEnrollmentCompletedAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var coursesCompleted = await db.Enrollments
            .CountAsync(e => e.StudentId == userId && e.Status == Domain.Enums.EnrollmentStatus.Completed, cancellationToken);

        var categoryId = await db.Courses
            .Where(c => c.Id == courseId)
            .Select(c => (Guid?)c.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);

        if (categoryId.HasValue)
            await categoryRepo.AddIfMissingAsync(userId, categoryId.Value, cancellationToken);

        // Save the category insert before counting so the count reflects it.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var distinctCategories = await categoryRepo.CountDistinctCategoriesAsync(userId, cancellationToken);

        var progress = await progressRepo.GetOrCreateAsync(userId, cancellationToken);
        progress.SetCoursesCompleted(coursesCompleted);
        progress.SetDistinctCategoriesCompleted(distinctCategories);

        await UnlockThresholdsAsync(userId, coursesCompleted, CourseThresholds, cancellationToken);

        if (distinctCategories >= AchievementCodes.PolymathMinCategories
            && !await achievementRepo.HasAchievementAsync(userId, AchievementCodes.Polymath, cancellationToken))
        {
            await achievementRepo.AddAsync(UserAchievement.Unlock(userId, AchievementCodes.Polymath), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task OnTestSubmittedAsync(
        Guid userId, int questionsCount, int durationSeconds, bool passed, CancellationToken cancellationToken)
    {
        if (!passed) return;
        if (questionsCount < AchievementCodes.SpeedDemonMinQuestions) return;
        if (durationSeconds >= AchievementCodes.SpeedDemonMaxDurationSeconds) return;

        if (await achievementRepo.HasAchievementAsync(userId, AchievementCodes.SpeedDemon, cancellationToken))
            return;

        await achievementRepo.AddAsync(UserAchievement.Unlock(userId, AchievementCodes.SpeedDemon), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task OnProfileChangedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Bio, u.AvatarBlobPath })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return;

        var profileComplete =
            !string.IsNullOrWhiteSpace(user.FirstName)
            && !string.IsNullOrWhiteSpace(user.LastName)
            && !string.IsNullOrWhiteSpace(user.Bio)
            && !string.IsNullOrWhiteSpace(user.AvatarBlobPath);

        var progress = await progressRepo.GetOrCreateAsync(userId, cancellationToken);
        progress.SetProfileCompleted(profileComplete);

        if (profileComplete
            && !await achievementRepo.HasAchievementAsync(userId, AchievementCodes.ProfileComplete, cancellationToken))
        {
            await achievementRepo.AddAsync(UserAchievement.Unlock(userId, AchievementCodes.ProfileComplete), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UnlockThresholdsAsync(
        Guid userId,
        int currentValue,
        (int Threshold, string Code)[] thresholds,
        CancellationToken cancellationToken)
    {
        foreach (var (threshold, code) in thresholds)
        {
            if (currentValue < threshold) continue;
            if (await achievementRepo.HasAchievementAsync(userId, code, cancellationToken)) continue;

            await achievementRepo.AddAsync(UserAchievement.Unlock(userId, code), cancellationToken);
        }
    }
}
