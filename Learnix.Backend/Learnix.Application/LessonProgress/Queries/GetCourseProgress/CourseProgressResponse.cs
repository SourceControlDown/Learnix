namespace Learnix.Application.LessonProgress.Queries.GetCourseProgress;

public sealed record CourseProgressResponse(
    Guid CourseId,
    int TotalLessons,
    int CompletedLessons,
    IReadOnlyList<SectionProgressDto> Sections);

public sealed record SectionProgressDto(
    Guid SectionId,
    string Title,
    int DisplayOrder,
    IReadOnlyList<LessonProgressItemDto> Lessons);

/// <param name="DurationSeconds">
/// A video's real length, or a post's estimated reading time. Null for a test, whose length is up
/// to the student, and for a video that was uploaded without a duration.
/// </param>
/// <param name="QuestionCount">
/// Only for a test — how many questions it asks, which is the closest thing it has to a length.
/// </param>
public sealed record LessonProgressItemDto(
    Guid LessonId,
    string Title,
    string LessonType,
    int DisplayOrder,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime? LastAccessedAt,
    int? DurationSeconds,
    int? QuestionCount);
