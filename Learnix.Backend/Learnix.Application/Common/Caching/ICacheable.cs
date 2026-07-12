namespace Learnix.Application.Common.Caching;

/// <summary>
/// Marks a query whose response should be served from (and written to) the distributed cache.
/// </summary>
/// <typeparam name="TValue">
/// The cached response type. It carries no members — <c>CachingBehavior</c> reads it off the
/// closed interface to know what to deserialize the cache entry into, so the type argument is
/// the whole point of the marker (S2326 does not apply).
/// </typeparam>
#pragma warning disable S2326
public interface ICacheable<TValue>
#pragma warning restore S2326
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
