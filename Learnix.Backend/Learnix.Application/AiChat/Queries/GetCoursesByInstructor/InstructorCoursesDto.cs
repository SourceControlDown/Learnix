namespace Learnix.Application.AiChat.Queries.GetCoursesByInstructor;

/// <summary>
/// Either the instructor was resolved (<paramref name="Instructor"/> + <paramref name="Courses"/>),
/// or the name matched several people and <paramref name="Ambiguous"/> lists them for the AI to disambiguate.
/// </summary>
public sealed record InstructorCoursesDto(
    InstructorSummaryDto? Instructor,
    IReadOnlyList<InstructorCourseDto>? Courses,
    IReadOnlyList<InstructorSummaryDto>? Ambiguous);

public sealed record InstructorSummaryDto(
    Guid InstructorId,
    string FullName,
    string? Bio);

public sealed record InstructorCourseDto(
    Guid CourseId,
    string Title,
    string ShortDescription,
    string CategoryName,
    decimal Price,
    bool IsFree,
    int EnrollmentsCount,
    decimal AverageRating,
    int ReviewsCount);
