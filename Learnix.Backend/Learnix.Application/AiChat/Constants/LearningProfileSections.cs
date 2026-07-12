namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// Sections the AI may request from <c>get_my_learning_profile</c>. Requesting a subset keeps the
/// tool result small when the AI only needs one part of the picture.
/// </summary>
public static class LearningProfileSections
{
    public const string Profile = "profile";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Wishlist = "wishlist";
    public const string Achievements = "achievements";

    public static IReadOnlyList<string> All { get; } = [Profile, InProgress, Completed, Wishlist, Achievements];
}
