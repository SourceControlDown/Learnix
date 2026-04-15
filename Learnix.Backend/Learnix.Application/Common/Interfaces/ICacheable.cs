namespace Learnix.Application.Common.Interfaces;

public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan Expiry { get; }
}
