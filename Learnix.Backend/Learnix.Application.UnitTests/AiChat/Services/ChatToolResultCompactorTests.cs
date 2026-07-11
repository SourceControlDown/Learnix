using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Services;

namespace Learnix.Application.UnitTests.AiChat.Services;

public class ChatToolResultCompactorTests
{
    private static readonly Guid LessonA = Guid.NewGuid();
    private static readonly Guid LessonB = Guid.NewGuid();

    [Fact]
    public void The_body_of_the_lesson_the_student_has_open_survives()
    {
        // Arrange
        var window = new List<ChatMessage>
        {
            User("what is this about?"),
            LessonResult("call-1", LessonA, "Closures explained at length")
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonA);

        // Assert
        Body(compacted, 1).Should().Contain("Closures explained at length");
    }

    [Fact]
    public void Moving_to_the_next_lesson_drops_the_previous_lesson_body()
    {
        // Arrange
        var window = new List<ChatMessage>
        {
            User("explain this"),
            LessonResult("call-1", LessonA, "Body of lesson A"),
            User("and now?"),
            LessonResult("call-2", LessonB, "Body of lesson B")
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonB);

        // Assert
        Body(compacted, 1).Should().NotContain("Body of lesson A");
        Body(compacted, 1).Should().Contain("Superseded");
        Body(compacted, 3).Should().Contain("Body of lesson B");
    }

    [Fact]
    public void Coming_back_to_a_lesson_keeps_one_copy_of_its_body_not_two()
    {
        // Arrange — the student opened A, went to B, came back to A and the model fetched A a second time
        var window = new List<ChatMessage>
        {
            User("explain this"),
            LessonResult("call-1", LessonA, "Body of lesson A"),
            User("and now?"),
            LessonResult("call-2", LessonB, "Body of lesson B"),
            User("back to the first one"),
            LessonResult("call-3", LessonA, "Body of lesson A")
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonA);

        // Assert
        Body(compacted, 1).Should().Contain("Superseded");
        Body(compacted, 3).Should().Contain("Superseded");
        Body(compacted, 5).Should().Contain("Body of lesson A");
    }

    [Fact]
    public void The_lesson_body_and_the_test_review_survive_side_by_side()
    {
        // Arrange
        var window = new List<ChatMessage>
        {
            User("how did I do?"),
            LessonResult("call-1", LessonA, "Rules of the test"),
            ToolResult(new ToolCall(
                "call-2",
                ChatToolNames.GetMyTestReview,
                "{}",
                Payload("testReview", LessonA, "\"title\":\"Checkpoint\"")))
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonA);

        // Assert
        Body(compacted, 1).Should().Contain("Rules of the test");
        Body(compacted, 2).Should().Contain("Checkpoint");
    }

    [Fact]
    public void With_no_lesson_open_every_stored_body_is_about_some_other_lesson()
    {
        // Arrange
        var window = new List<ChatMessage>
        {
            User("explain this"),
            LessonResult("call-1", LessonA, "Body of lesson A")
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, currentLessonId: null);

        // Assert
        Body(compacted, 1).Should().Contain("Superseded");
    }

    [Fact]
    public void An_error_result_is_never_mistaken_for_a_live_body()
    {
        // Arrange
        var window = new List<ChatMessage>
        {
            User("explain this"),
            ToolResult(new ToolCall(
                "call-1",
                ChatToolNames.GetCurrentLesson,
                "{}",
                """{"error":"The student does not have a lesson open."}"""))
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonA);

        // Assert
        Body(compacted, 1).Should().Contain("Superseded");
    }

    [Fact]
    public void Results_of_tools_that_are_not_about_a_lesson_are_left_alone()
    {
        // Arrange
        var window = new List<ChatMessage>
        {
            User("how do certificates work?"),
            ToolResult(new ToolCall("call-1", ChatToolNames.GetPlatformInfo, "{}", """{"topic":"certificates"}"""))
        };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonB);

        // Assert
        compacted[1].Should().BeSameAs(window[1]);
        Body(compacted, 1).Should().Contain("certificates");
    }

    [Fact]
    public void Plain_messages_pass_through_untouched()
    {
        // Arrange
        var window = new List<ChatMessage> { User("hello"), Assistant("hi") };

        // Act
        var compacted = ChatToolResultCompactor.Compact(window, LessonA);

        // Assert
        compacted.Should().Equal(window);
    }

    private static string Body(IReadOnlyList<ChatMessage> window, int index) =>
        window[index].ToolCalls![0].ResultJson!;

    private static ChatMessage User(string content) => new("user", content, DateTime.UtcNow);

    private static ChatMessage Assistant(string content) => new("assistant", content, DateTime.UtcNow);

    private static ChatMessage ToolResult(params ToolCall[] calls) =>
        new("tool_result", string.Empty, DateTime.UtcNow, calls);

    private static ChatMessage LessonResult(string callId, Guid lessonId, string content) =>
        ToolResult(new ToolCall(
            callId,
            ChatToolNames.GetCurrentLesson,
            "{}",
            Payload("currentLesson", lessonId, $"\"content\":\"{content}\"")));

    /// <summary>The shape a lesson-bound tool writes: one named object carrying a lessonId.</summary>
    private static string Payload(string root, Guid lessonId, string fields) =>
        $"{{\"{root}\":{{\"lessonId\":\"{lessonId}\",{fields}}}}}";
}
