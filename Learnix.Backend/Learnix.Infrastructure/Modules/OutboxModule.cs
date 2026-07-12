using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.Handlers;
using Learnix.Infrastructure.Services.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>
/// Outbox: the message handlers, and the worker that drains the table
/// (PostgreSQL LISTEN/NOTIFY + FOR UPDATE SKIP LOCKED).
/// </summary>
public static class OutboxModule
{
    public static IServiceCollection AddOutbox(this IServiceCollection services)
    {
        services.AddScoped<OutboxDbContextHolder>();
        services.AddOutboxMessageHandlers();

        services.AddSingleton<OutboxSignal>();
        services.AddHostedService<OutboxNotificationListener>();
        services.AddHostedService<OutboxProcessorService>();

        return services;
    }
}
