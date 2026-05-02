using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services;

public sealed class CategorySeederHostedService(
    IServiceProvider serviceProvider,
    ILogger<CategorySeederHostedService> logger) : IHostedService
{
    private static readonly (string Name, string Slug)[] SeedCategories =
    [
        ("Programming", "programming"),
        ("Web Development", "web-development"),
        ("Data Science", "data-science"),
        ("Design", "design"),
        ("Business", "business"),
        ("Marketing", "marketing"),
        ("Personal Development", "personal-development"),
        ("Language Learning", "language-learning"),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingSlugs = await context.Categories
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);

        var toInsert = SeedCategories
            .Where(sc => !existingSlugs.Contains(sc.Slug))
            .Select(sc => Category.CreateSystem(sc.Name, sc.Slug))
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogInformation("Category seed: nothing to add.");
            return;
        }

        await context.Categories.AddRangeAsync(toInsert, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category seed: inserted {Count} system categories.", toInsert.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}