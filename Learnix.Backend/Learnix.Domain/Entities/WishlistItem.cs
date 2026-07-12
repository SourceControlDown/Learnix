using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

public class WishlistItem : IAuditable
{
    private WishlistItem() { }

    private WishlistItem(Guid userId, Guid courseId)
    {
        UserId = userId;
        CourseId = courseId;
    }

    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }

    // S1144: the setters are never called from code — EF Core materializes these.
#pragma warning disable S1144
    public Course? Course { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
#pragma warning restore S1144

    public static WishlistItem Create(Guid userId, Guid courseId)
        => new(userId, courseId);
}
