using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Infrastructure.Identity;

internal sealed class UserRoleService(UserManager<User> userManager) : IUserRoleService
{
    public async Task AssignRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null) return;

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
