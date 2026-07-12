using Learnix.Infrastructure.Services.HostedServices.Cleanup;
using Learnix.Infrastructure.Services.HostedServices.Maintenance;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>
/// Scheduled background work: cleanup of expired data and reconciliation of denormalized counters.
/// The Outbox worker lives in <see cref="OutboxModule"/>; the Mongo index initializer in <see cref="MongoModule"/>.
/// </summary>
public static class BackgroundJobsModule
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<RefreshTokenCleanupHostedService>();
        services.AddHostedService<DeletedAccountPurgeService>();

        services.AddHostedService<CategoryCoursesCountReconciliationService>();
        services.AddHostedService<CourseRatingReconciliationService>();

        return services;
    }
}
