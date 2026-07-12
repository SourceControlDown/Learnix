namespace Learnix.Infrastructure.Outbox;

/// <summary>
/// In-process signal that wakes the <see cref="Services.OutboxProcessorService"/>
/// when new outbox messages are available. Registered as singleton.
/// <para>
/// The <see cref="Services.OutboxNotificationListener"/> calls <see cref="Notify"/>
/// when a PostgreSQL LISTEN/NOTIFY notification arrives on the <c>outbox_new</c> channel.
/// The processor awaits <see cref="WaitAsync"/> which returns immediately on signal
/// or after the fallback timeout — whichever comes first.
/// </para>
/// </summary>
internal sealed class OutboxSignal
{
    private readonly SemaphoreSlim _semaphore = new(0);

    /// <summary>Signal the processor that new messages are available.</summary>
    public void Notify() => _semaphore.Release();

    /// <summary>
    /// Wait until signalled or <paramref name="timeout"/> expires.
    /// Returns <c>true</c> if signalled, <c>false</c> on timeout.
    /// </summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => _semaphore.WaitAsync(timeout, cancellationToken);

    /// <summary>
    /// Collapses every signal queued up behind the one just consumed, and reports how many there were.
    /// </summary>
    /// <remarks>
    /// A <see cref="SemaphoreSlim"/> counts: five commits during one batch leave five permits, and the
    /// processor would then run five more iterations — each a separate <c>SELECT ... FOR UPDATE SKIP
    /// LOCKED</c> — to learn what the first one already told it. The signal carries no information beyond
    /// "something is there", so N of them mean exactly what one means.
    ///
    /// Draining is safe because the caller drains <b>before</b> it queries: any row committed before that
    /// query is in its result set regardless of how many permits announced it, and any row committed after
    /// it raises a fresh notification that arrives after the drain. The 10-second fallback backs the whole
    /// thing up if a signal is ever genuinely lost.
    /// </remarks>
    public int DrainPending()
    {
        var drained = 0;
        while (_semaphore.Wait(0))
            drained++;

        return drained;
    }
}
