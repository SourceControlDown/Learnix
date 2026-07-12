using Learnix.Infrastructure.Outbox;

namespace Learnix.Infrastructure.UnitTests.Outbox;

/// <summary>
/// The signal carries one bit of information — "there is something in the outbox". These tests pin the
/// consequence of that: N notifications must cost the processor one query, not N.
/// </summary>
public class OutboxSignalTests
{
    private static readonly TimeSpan NoWait = TimeSpan.Zero;

    [Fact]
    public async Task WaitAsync_ReturnsImmediately_WhenAlreadySignalled()
    {
        var signal = new OutboxSignal();

        signal.Notify();

        (await signal.WaitAsync(NoWait, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task WaitAsync_TimesOut_WhenNotSignalled()
    {
        var signal = new OutboxSignal();

        (await signal.WaitAsync(NoWait, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Signals_Accumulate_WithoutDraining()
    {
        var signal = new OutboxSignal();

        // Five commits during one batch — the semaphore counts them.
        for (var i = 0; i < 5; i++)
            signal.Notify();

        // Consuming one leaves four. This is the behaviour DrainPending exists to collapse: without it the
        // processor would run four more batches to be told what the first one already knew.
        (await signal.WaitAsync(NoWait, CancellationToken.None)).Should().BeTrue();

        signal.DrainPending().Should().Be(4);
    }

    [Fact]
    public async Task DrainPending_LeavesNothingBehind()
    {
        var signal = new OutboxSignal();

        for (var i = 0; i < 5; i++)
            signal.Notify();

        await signal.WaitAsync(NoWait, CancellationToken.None);
        signal.DrainPending();

        // Nothing left: the next iteration must wait for a genuinely new notification (or the fallback),
        // not spin through stale permits.
        (await signal.WaitAsync(NoWait, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public void DrainPending_ReturnsZero_WhenNothingIsQueued()
    {
        var signal = new OutboxSignal();

        signal.DrainPending().Should().Be(0);
    }

    [Fact]
    public async Task NotificationAfterDraining_StillWakesTheProcessor()
    {
        var signal = new OutboxSignal();

        signal.Notify();
        await signal.WaitAsync(NoWait, CancellationToken.None);
        signal.DrainPending();

        // A row committed after the drain raises its own notification, and that one must survive — this is
        // what makes draining safe rather than lossy.
        signal.Notify();

        (await signal.WaitAsync(NoWait, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task WaitAsync_Throws_WhenCancelled()
    {
        var signal = new OutboxSignal();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var wait = async () => await signal.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);

        await wait.Should().ThrowAsync<OperationCanceledException>();
    }
}
