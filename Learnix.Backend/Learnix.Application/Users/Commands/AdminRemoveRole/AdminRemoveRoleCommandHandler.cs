using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminRemoveRole;

internal sealed class AdminRemoveRoleCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUserRoleService roleService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminRemoveRoleCommand, Result>
{
    public async Task<Result> Handle(AdminRemoveRoleCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can change user roles."));

        if (!Roles.All.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return Result.Fail(new Error($"'{request.Role}' is not a valid role."));

        // Student is the base role every user always has. Removing it leaves the user in a broken state.
        if (request.Role.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase))
            return Result.Fail(new ConflictError("The Student role is the base role and cannot be removed."));

        var user = await userRepository.FirstOrDefaultAsync(
            new AdminUserByIdSpecification(request.UserId, forUpdate: true),
            cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError($"User {request.UserId} not found."));

        var currentRoles = await roleService.GetRolesAsync(request.UserId, cancellationToken);
        if (!currentRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return Result.Fail(new ConflictError($"User does not have the '{request.Role}' role."));

        await roleService.RemoveRoleAsync(request.UserId, request.Role, cancellationToken);

        user.RaiseRoleChanged(request.Role, assigned: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
