namespace Learnix.Application.Enrollments.Queries.GetContinueLearning;

/// <param name="LessonId">The lesson to open — the first one the student has not completed.</param>
public sealed record ContinueLearningDto(
    Guid CourseId,
    string CourseTitle,
    Guid LessonId);
