namespace Learnix.Application.AiChat.Constants;

public static class AiChatMessages
{
    public const string InstructorNotFound = "No instructor matches the given name or id.";

    public static string UnknownSection(string section) =>
        $"Unknown section '{section}'. Allowed sections: {string.Join(", ", LearningProfileSections.All)}.";

    public const string InstructorLookupRequired =
        "Either instructorName or instructorId must be provided.";
}
