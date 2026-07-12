using System.Reflection;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.Handlers;

namespace Learnix.Infrastructure.UnitTests.Outbox;

public class OutboxMessageDispatcherTests
{
    /// <summary>
    /// The dispatcher refuses to start unless every declared type is claimed, so a test that builds it with
    /// a partial handler set has to say which types it is deliberately leaving out.
    /// </summary>
    private static OutboxMessageDispatcher DispatcherWith(params IOutboxMessageHandler[] handlers)
    {
        var declared = DeclaredMessageTypes();
        var claimed = handlers.Select(h => h.MessageType).ToHashSet();
        var stubs = declared.Where(t => !claimed.Contains(t)).Select(t => new StubHandler(t));

        return new OutboxMessageDispatcher([.. handlers, .. stubs]);
    }

    private static List<string> DeclaredMessageTypes() =>
        typeof(OutboxMessageTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

    private static OutboxMessage MessageOf(string type) =>
        OutboxMessage.Create(Guid.NewGuid(), type, new { });

    [Fact]
    public async Task A_message_goes_to_the_handler_that_owns_its_type_and_to_no_other()
    {
        // Arrange
        var target = new StubHandler(OutboxMessageTypes.PasswordResetEmail);
        var bystander = new StubHandler(OutboxMessageTypes.EmailConfirmation);
        var sut = DispatcherWith(target, bystander);

        // Act
        await sut.DispatchAsync(MessageOf(OutboxMessageTypes.PasswordResetEmail), CancellationToken.None);

        // Assert
        target.Handled.Should().Be(1);
        bystander.Handled.Should().Be(0);
    }

    /// <summary>
    /// An unknown type must reach the processor's retry-and-log path. Swallowing it would turn a message
    /// nobody delivers into a message nobody misses.
    /// </summary>
    [Fact]
    public async Task An_unknown_message_type_throws_rather_than_being_skipped()
    {
        var sut = DispatcherWith();

        var act = () => sut.DispatchAsync(MessageOf("SomethingNobodyHandles"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SomethingNobodyHandles*");
    }

    [Fact]
    public void Two_handlers_claiming_one_type_is_a_startup_failure_not_a_coin_toss()
    {
        var act = () => DispatcherWith(
            new StubHandler(OutboxMessageTypes.DeleteBlob),
            new StubHandler(OutboxMessageTypes.DeleteBlob));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OutboxMessageTypes.DeleteBlob}*");
    }

    /// <summary>
    /// The guard the switch never had: a type that can be enqueued but not delivered is a side-effect that
    /// silently never happens. It now costs a failed startup instead of a lost password reset.
    /// </summary>
    [Fact]
    public void A_declared_message_type_with_no_handler_stops_the_application_from_starting()
    {
        var everyTypeButOne = DeclaredMessageTypes()
            .Where(t => t != OutboxMessageTypes.AccountDeletedEmail)
            .Select(t => new StubHandler(t))
            .ToList();

        var act = () => new OutboxMessageDispatcher(everyTypeButOne);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{OutboxMessageTypes.AccountDeletedEmail}*");
    }

    private sealed class StubHandler(string messageType) : IOutboxMessageHandler
    {
        public string MessageType => messageType;

        public int Handled { get; private set; }

        public Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
        {
            Handled++;
            return Task.CompletedTask;
        }
    }
}
