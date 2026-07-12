using System.Reflection;
using Learnix.Application.Common.Options;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure;

/// <summary>
/// Composition root of the Infrastructure layer. Each concern is registered by its own module
/// under <c>Modules/</c>; this file only composes them.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppOptions>(configuration.GetSection(ConfigurationSectionNameConstants.App));

        // Handlers that live in this assembly: outbox message handlers and domain event handlers.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services
            .AddPersistence(configuration)
            .AddMongo(configuration)
            .AddCaching(configuration)
            .AddStorage(configuration)
            .AddAuth(configuration)
            .AddEmail(configuration)
            .AddOutbox()
            .AddAiChat(configuration)
            .AddCertificates()
            .AddCatalog()
            .AddBackgroundJobs();

        return services;
    }
}
