using Learnix.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services.HostedServices.Seeders;

internal sealed class RoleSeederHostedService(
    IServiceProvider serviceProvider,
    ILogger<RoleSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (result.Succeeded)
                    logger.LogInformation("Seeded role: {Role}", roleName);
                else
                    logger.LogError("Failed to seed role {Role}: {Errors}", roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}