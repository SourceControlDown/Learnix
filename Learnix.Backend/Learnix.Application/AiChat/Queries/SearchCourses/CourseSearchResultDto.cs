namespace Learnix.Application.AiChat.Queries.SearchCourses;

public sealed record CourseSearchResultDto(
    Guid CourseId,
    string Title,
    string ShortDescription,
    string CategoryId,
    decimal Price,
    bool IsFree,
    int EnrollmentsCount);
