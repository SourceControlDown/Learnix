namespace Learnix.Application.LessonProgress.Abstractions;

/// <param name="CompletedLessons">Visible lessons the student has completed.</param>
/// <param name="TotalLessons">Visible lessons in the course. Zero for a course with no published lessons.</param>
public sealed record CourseProgressCounts(int CompletedLessons, int TotalLessons);
