namespace Learnix.Application.Wishlist.Queries.GetMyWishlist;

public sealed record WishlistCourseDto(
    Guid CourseId,
    string CourseTitle,
    string? CourseCoverBlobPath,
    decimal CoursePrice,
    bool IsFree,
    DateTime AddedAt);
