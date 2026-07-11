using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Abstractions;

/// <summary>
/// What the platform last learned about the AI provider, from the chat traffic itself (ADR-CHAT-014).
/// <para>
/// The status is never probed: on a free tier the quota is counted in requests, so a health-check ping would
/// consume exactly what it is checking for. Instead every real chat turn reports what happened, and the
/// answer is remembered until the provider is worth calling again.
/// </para>
/// </summary>
public interface IAiAvailabilityStore
{
    /// <summary>The outage in force, or null when the provider is believed healthy.</summary>
    Task<AiOutage?> GetOutageAsync(CancellationToken ct = default);

    /// <summary>A turn completed. Clears any outage — the provider answered, so it is up.</summary>
    Task ReportSuccessAsync(CancellationToken ct = default);

    /// <summary>A turn failed. The outage stands until <see cref="AiOutage.RetryAtUtc"/>.</summary>
    Task ReportOutageAsync(AiOutage outage, CancellationToken ct = default);
}
