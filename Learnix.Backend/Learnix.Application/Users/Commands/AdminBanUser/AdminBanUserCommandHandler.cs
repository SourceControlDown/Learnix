using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminBanUser;

internal sealed class AdminBanUserCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminBanUserCommand, Result>
{
    public async Task<Result> Handle(AdminBanUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can ban users."));

        if (currentUser.UserId == request.UserId)
            return Result.Fail(new ConflictError("Admins cannot ban themselves."));

        var user = await userRepository.FirstOrDefaultAsync(
            new AdminUserByIdSpecification(request.UserId, forUpdate: true),
            cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError($"User {request.UserId} not found."));

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            return Result.Fail(new ConflictError("User is already banned."));

        user.Ban();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
