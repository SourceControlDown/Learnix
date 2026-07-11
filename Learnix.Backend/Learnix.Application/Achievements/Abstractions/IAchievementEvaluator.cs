namespace Learnix.Application.Achievements.Abstractions;

/// <summary>
/// Encapsulates achievement rules. Invoked by the outbox processor when
/// achievement-related domain events are dispatched. Each method updates
/// counters/state and unlocks any achievements whose thresholds were just crossed.
/// </summary>
public interface IAchievementEvaluator
{
    Task OnLessonCompletedAsync(Guid userId, CancellationToken cancellationToken);
    Task OnEnrollmentCompletedAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task OnTestSubmittedAsync(Guid userId, int questionsCount, int durationSeconds, bool passed, CancellationToken cancellationToken);
    Task OnProfileChangedAsync(Guid userId, CancellationToken cancellationToken);
}
