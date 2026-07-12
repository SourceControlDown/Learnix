namespace Learnix.Application.AiChat.Queries.SearchCourses;

public sealed record CourseSearchResultDto(
    Guid CourseId,
    string Title,
    string ShortDescription,
    string CategoryName,
    Guid InstructorId,
    string InstructorFullName,
    decimal Price,
    bool IsFree,
    int EnrollmentsCount);
