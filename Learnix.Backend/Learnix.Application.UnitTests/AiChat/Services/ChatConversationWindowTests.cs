using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Services;

namespace Learnix.Application.UnitTests.AiChat.Services;

public class ChatConversationWindowTests
{
    private static ChatMessage User(string content = "q") =>
        new("user", content, DateTime.UtcNow);

    private static ChatMessage Assistant(string content = "a") =>
        new("assistant", content, DateTime.UtcNow);

    private static ChatMessage AssistantToolUse(string callId) =>
        new("assistant", string.Empty, DateTime.UtcNow, [new ToolCall(callId, "search_courses", "{}")]);

    private static ChatMessage ToolResult(string callId) =>
        new("tool_result", string.Empty, DateTime.UtcNow, [new ToolCall(callId, "search_courses", "{}", "[]")]);

    private static bool OpensLegally(IReadOnlyList<ChatMessage> window) =>
        window[0].Role == "user" && window[0].ToolCalls is null or { Count: 0 };

    private static readonly List<ChatMessage> ToolTurnFollowedByPlainTurn =
    [
        User("q0"),
        AssistantToolUse("call-1"),
        ToolResult("call-1"),
        Assistant("a0"),
        User("q1"),
        Assistant("a1"),
    ];

    [Fact]
    public void Returns_conversation_unchanged_when_shorter_than_the_window()
    {
        var conversation = new List<ChatMessage> { User(), Assistant() };

        var window = ChatConversationWindow.TakeAlignedWindow(conversation, 20);

        window.Should().BeEquivalentTo(conversation, o => o.WithStrictOrdering());
    }

    [Fact]
    public void Trims_to_the_window_size_on_a_conversation_without_tools()
    {
        var conversation = new List<ChatMessage>();
        for (var i = 0; i < 5; i++)
        {
            conversation.Add(User($"q{i}"));
            conversation.Add(Assistant($"a{i}"));
        }

        var window = ChatConversationWindow.TakeAlignedWindow(conversation, 4);

        window.Should().HaveCount(4);
        window[0].Content.Should().Be("q3");
        OpensLegally(window).Should().BeTrue();
    }

    [Fact]
    public void Drops_a_tool_result_whose_tool_call_was_cut_away()
    {
        // TakeLast(4) opens on the orphaned tool_result — Anthropic would reject the request.
        var window = ChatConversationWindow.TakeAlignedWindow(ToolTurnFollowedByPlainTurn, 4);

        OpensLegally(window).Should().BeTrue();
        window.Should().NotContain(m => m.Role == "tool_result");
        window[0].Content.Should().Be("q1");
    }

    [Fact]
    public void Drops_an_assistant_tool_call_left_without_its_user_turn()
    {
        // TakeLast(5) opens on the assistant that requested the call, one message earlier.
        var window = ChatConversationWindow.TakeAlignedWindow(ToolTurnFollowedByPlainTurn, 5);

        OpensLegally(window).Should().BeTrue();
        window[0].Content.Should().Be("q1");
        window.Should().HaveCount(2);
    }

    [Fact]
    public void Keeps_a_tool_turn_whole_when_the_window_starts_on_its_user_message()
    {
        var conversation = new List<ChatMessage>
        {
            User("q0"),
            Assistant("a0"),
            User("q1"),
            AssistantToolUse("call-1"),
            ToolResult("call-1"),
            Assistant("a1"),
        };

        var window = ChatConversationWindow.TakeAlignedWindow(conversation, 4);

        window.Should().HaveCount(4);
        window[0].Content.Should().Be("q1");
        window.Should().Contain(m => m.Role == "tool_result");
    }

    [Fact]
    public void Widens_back_to_the_last_user_turn_when_the_window_is_smaller_than_a_tool_turn()
    {
        var conversation = new List<ChatMessage>
        {
            User("q0"),
            AssistantToolUse("call-1"),
            ToolResult("call-1"),
        };

        var window = ChatConversationWindow.TakeAlignedWindow(conversation, 2);

        OpensLegally(window).Should().BeTrue();
        window.Should().HaveCount(3);
    }

    [Fact]
    public void Returns_empty_for_an_empty_conversation()
    {
        ChatConversationWindow.TakeAlignedWindow([], 20).Should().BeEmpty();
    }
}
