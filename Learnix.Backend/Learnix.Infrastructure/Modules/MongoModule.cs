using Learnix.Application.AiChat.Abstractions;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Persistence.Mongo;
using Learnix.Infrastructure.Persistence.Mongo.Conventions;
using Learnix.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Learnix.Infrastructure.Modules;

/// <summary>MongoDB: client, context, AI chat session storage and index initialization.</summary>
public static class MongoModule
{
    public static IServiceCollection AddMongo(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoOptions>(configuration.GetSection(ConfigurationSectionNameConstants.Mongo));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            MongoConventionRegistration.Register();
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }
}
