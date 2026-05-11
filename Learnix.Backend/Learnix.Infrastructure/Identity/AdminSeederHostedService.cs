using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Identity;

internal sealed class AdminSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<AdminSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "SeedAdmin:Email or SeedAdmin:Password is not configured — skipping admin seeding. " +
                "Set both values to create the initial admin account on startup.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var usersInAdminRole = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (usersInAdminRole.Count > 0)
            return;

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // User exists but has no Admin role yet — just promote them.
            var addResult = await userManager.AddToRoleAsync(existing, Roles.Admin);
            if (addResult.Succeeded)
                logger.LogInformation("Promoted existing user {Email} to Admin.", email);
            else
                logger.LogError("Failed to promote {Email}: {Errors}", email, string.Join("; ", addResult.Errors.Select(e => e.Description)));
            return;
        }

        var admin = new User(email, "System", "Administrator")
        {
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create seed admin {Email}: {Errors}", email,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (roleResult.Succeeded)
            logger.LogInformation("Seeded admin account: {Email}", email);
        else
            logger.LogError("Admin user created but role assignment failed: {Errors}",
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
