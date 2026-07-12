namespace Learnix.Application.AiChat.Abstractions;

public static class AiChatConstants
{
    /// <summary>
    /// Deliberately larger than <see cref="Domain.Constants.ConversationConstants.MessageMaxLength"/>:
    /// a question to the tutor may legitimately carry a code snippet or a problem statement.
    /// </summary>
    public const int MessageMaxLength = 4000;
}
