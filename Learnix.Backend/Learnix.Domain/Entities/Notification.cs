using Learnix.Domain.Common;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

/// <summary>
/// A thing that happened, addressed to one user — not a sentence about it. The bell renders whatever the
/// client makes of <see cref="Type"/> and <see cref="Parameters"/>; the server never picks the words
/// (ADR-BACK-NOTIF-001).
/// </summary>
public sealed class Notification : BaseEntity
{
    private Notification() { }

    private Notification(Guid userId, NotificationType type, string? parameters)
    {
        UserId = userId;
        Type = type;
        Parameters = parameters;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }

    /// <summary>
    /// What the client needs in order to name the thing: a course title, an achievement code. A flat JSON
    /// object of strings — or null, when the type already says everything ("your application was approved").
    /// </summary>
    public string? Parameters { get; private set; }

    public bool IsRead { get; private set; }

    public static Notification Create(Guid userId, NotificationType type, string? parameters = null)
        => new(userId, type, parameters);

    public void MarkRead() => IsRead = true;
}
