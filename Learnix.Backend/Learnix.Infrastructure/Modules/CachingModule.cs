using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>Redis distributed cache, backing CachingBehavior and the AI availability store.</summary>
public static class CachingModule
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");
        });

        return services;
    }
}
