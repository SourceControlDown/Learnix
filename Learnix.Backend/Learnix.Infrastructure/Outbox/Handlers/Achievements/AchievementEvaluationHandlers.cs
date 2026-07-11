using Learnix.Application.Achievements.Abstractions;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.Outbox.Handlers.Achievements;

/// <summary>
/// Re-evaluates a student's achievements after something they did. Deliberately out of band: the lesson,
/// the test and the profile edit are committed before any of this runs.
/// </summary>
internal sealed class EvaluateLessonCompletedHandler(IAchievementEvaluator evaluator)
    : OutboxMessageHandler<EvaluateLessonCompletedPayload>
{
    public override string MessageType => OutboxMessageTypes.EvaluateLessonCompleted;

    protected override Task HandleAsync(EvaluateLessonCompletedPayload payload, CancellationToken cancellationToken) =>
        evaluator.OnLessonCompletedAsync(payload.UserId, cancellationToken);
}

internal sealed class EvaluateEnrollmentCompletedHandler(IAchievementEvaluator evaluator)
    : OutboxMessageHandler<EvaluateEnrollmentCompletedPayload>
{
    public override string MessageType => OutboxMessageTypes.EvaluateEnrollmentCompleted;

    protected override Task HandleAsync(EvaluateEnrollmentCompletedPayload payload, CancellationToken cancellationToken) =>
        evaluator.OnEnrollmentCompletedAsync(payload.UserId, payload.CourseId, cancellationToken);
}

internal sealed class EvaluateTestSubmittedHandler(IAchievementEvaluator evaluator)
    : OutboxMessageHandler<EvaluateTestSubmittedPayload>
{
    public override string MessageType => OutboxMessageTypes.EvaluateTestSubmitted;

    protected override Task HandleAsync(EvaluateTestSubmittedPayload payload, CancellationToken cancellationToken) =>
        evaluator.OnTestSubmittedAsync(
            payload.UserId, payload.QuestionsCount, payload.DurationSeconds, payload.Passed, cancellationToken);
}

internal sealed class EvaluateProfileChangedHandler(IAchievementEvaluator evaluator)
    : OutboxMessageHandler<EvaluateProfileChangedPayload>
{
    public override string MessageType => OutboxMessageTypes.EvaluateProfileChanged;

    protected override Task HandleAsync(EvaluateProfileChangedPayload payload, CancellationToken cancellationToken) =>
        evaluator.OnProfileChangedAsync(payload.UserId, cancellationToken);
}
