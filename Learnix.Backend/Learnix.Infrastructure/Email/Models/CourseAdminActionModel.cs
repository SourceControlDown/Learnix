namespace Learnix.Infrastructure.Email.Models;

internal sealed class CourseAdminActionModel
{
    public required string InstructorFirstName { get; init; }
    public required string CourseTitle { get; init; }
}
