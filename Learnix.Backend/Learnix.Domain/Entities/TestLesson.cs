using Learnix.Domain.Constants;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public class TestLesson : Lesson
{
    private TestLesson() { }

    private TestLesson(
        Guid sectionId,
        string title,
        int order,
        string? description,
        int? attemptLimit,
        int? cooldownMinutes,
        int passingThreshold)
        : base(sectionId, title, order, LessonType.Test)
    {
        Description = description;
        AttemptLimit = attemptLimit;
        CooldownMinutes = cooldownMinutes;
        PassingThreshold = passingThreshold;
    }

    public string? Description { get; private set; }
    public int? AttemptLimit { get; private set; }
    public int? CooldownMinutes { get; private set; }
    public int PassingThreshold { get; private set; }

    public static TestLesson Create(
        Guid sectionId,
        string title,
        int order,
        string? description = null,
        int? attemptLimit = null,
        int? cooldownMinutes = null,
        int passingThreshold = LessonConstants.DefaultPassingThreshold)
        => new(sectionId, title, order, description, attemptLimit, cooldownMinutes, passingThreshold);

    public void UpdateSettings(
        string? description,
        int? attemptLimit,
        int? cooldownMinutes,
        int passingThreshold)
    {
        Description = description;
        AttemptLimit = attemptLimit;
        CooldownMinutes = cooldownMinutes;
        PassingThreshold = passingThreshold;
    }
}