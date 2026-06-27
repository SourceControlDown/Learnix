namespace Learnix.Application.Common.Constants;

public static class CacheKeys
{
    public static string PopularCourses => "popular-courses";
    public static string Course(Guid id) => $"course:{id}";
    public static string CoursesFeatured => "courses:featured";
    public static string UserAchievements(Guid userId) => $"user-achievements:{userId}";
    public static string CategoriesAll => "categories:all";
}
