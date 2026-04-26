using Learnix.Application.Common.Events;
using Learnix.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Persistence.Interceptors;

public class DomainEventsInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var dbContext = eventData.Context;
        
        if (dbContext is null) 
            return await base.SavingChangesAsync(eventData, result, ct);

        var entities = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        var events = entities.SelectMany(e => e.Entity.DomainEvents).ToList();
        
        foreach (var entry in entities) 
            entry.Entity.ClearDomainEvents();

        // Resolve publisher per save (interceptor scoped, but be safe)
        using var scope = serviceProvider.CreateScope();
        
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DomainEventsInterceptor>>();

        foreach (var @event in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(@event.GetType());
            var notification = Activator.CreateInstance(notificationType, @event)!;

            try
            {
                await publisher.Publish(notification, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Domain event handler failed for {EventType}", @event.GetType().Name);
            }
        }

        return await base.SavingChangesAsync(eventData, result, ct);
    }
}
