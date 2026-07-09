namespace Learnix.Application.AiChat.Queries.GetMyLearningProfile;

/// <summary>Sections the caller did not ask for are null and are omitted from the tool result.</summary>
public sealed record MyLearningProfileDto(
    ProfileSummaryDto? Profile,
    LearningProfileSection<InProgressCourseDto>? InProgress,
    LearningProfileSection<CompletedCourseDto>? Completed,
    LearningProfileSection<WishlistCourseAiDto>? Wishlist,
    AchievementsSummaryDto? Achievements);

/// <param name="Total">Total matching rows, before the cap.</param>
/// <param name="Truncated">True when <paramref name="Items"/> holds only the first page of <paramref name="Total"/>.</param>
public sealed record LearningProfileSection<T>(
    int Total,
    bool Truncated,
    IReadOnlyList<T> Items);

public sealed record ProfileSummaryDto(
    string FirstName,
    string LastName,
    string Email,
    string? Bio,
    IReadOnlyList<string> Roles,
    DateTime JoinedAt);

public sealed record InProgressCourseDto(
    Guid CourseId,
    string Title,
    string CategoryName,
    int CompletedLessons,
    int TotalLessons,
    int ProgressPercent,
    DateTime EnrolledAt);

public sealed record CompletedCourseDto(
    Guid CourseId,
    string Title,
    string CategoryName,
    DateTime? CompletedAt);

public sealed record WishlistCourseAiDto(
    Guid CourseId,
    string Title,
    decimal Price,
    bool IsFree,
    DateTime AddedAt);

public sealed record AchievementsSummaryDto(
    int UnlockedCount,
    IReadOnlyList<string> UnlockedCodes,
    int LessonsCompleted,
    int CoursesCompleted,
    int DistinctCategoriesCompleted,
    bool ProfileCompleted);
