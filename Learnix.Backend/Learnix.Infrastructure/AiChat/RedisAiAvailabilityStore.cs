using System.Text.Json;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.Common.Constants;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.AiChat;

/// <summary>
/// The outage lives in Redis and nowhere else. It is shared by every instance — one student's 429 spares the
/// next student the same wasted request — and its own expiry ends it: the key is written with a TTL that runs
/// to <see cref="AiOutage.RetryAtUtc"/>, so nothing has to remember to clear it.
/// </summary>
internal sealed class RedisAiAvailabilityStore(
    IDistributedCache cache,
    ILogger<RedisAiAvailabilityStore> logger) : IAiAvailabilityStore
{
    /// <summary>An outage with no stated end still expires: the provider may recover without telling us.</summary>
    private static readonly TimeSpan MaxOutage = TimeSpan.FromHours(1);

    public async Task<AiOutage?> GetOutageAsync(CancellationToken ct = default)
    {
        var payload = await cache.GetStringAsync(CacheKeys.AiChat.Outage, ct);

        return payload is null ? null : JsonSerializer.Deserialize<AiOutage>(payload);
    }

    public Task ReportSuccessAsync(CancellationToken ct = default) =>
        cache.RemoveAsync(CacheKeys.AiChat.Outage, ct);

    public async Task ReportOutageAsync(AiOutage outage, CancellationToken ct = default)
    {
        var ttl = outage.RetryAtUtc is { } retryAt
            ? retryAt - DateTime.UtcNow
            : MaxOutage;

        // A retry time already in the past is not an outage worth remembering.
        if (ttl <= TimeSpan.Zero)
            return;

        logger.LogWarning(
            "AI provider unavailable ({Reason}) until {RetryAt}: {Message}",
            outage.Reason,
            outage.RetryAtUtc,
            outage.Message);

        await cache.SetStringAsync(
            CacheKeys.AiChat.Outage,
            JsonSerializer.Serialize(outage),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Min(ttl, MaxOutage) },
            ct);
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}
