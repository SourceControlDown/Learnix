using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Users.Queries.GetAdminUsers;

internal sealed class GetAdminUsersQueryHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUserRoleService roleService,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetAdminUsersQuery, Result<PaginatedResult<AdminUserDto>>>
{
    public async Task<Result<PaginatedResult<AdminUserDto>>> Handle(
        GetAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can list users."));

        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await userRepository.CountAsync(
            new AdminUserListCountSpecification(request.Search),
            cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<AdminUserDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var users = await userRepository.ListAsync(
            new AdminUserListSpecification(request.Search, pagination.Skip, pagination.Take),
            cancellationToken);

        var dtos = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await roleService.GetRolesAsync(user.Id, cancellationToken);
            dtos.Add(new AdminUserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.AvatarBlobPath is not null
                    ? blobStorage.GenerateReadUrl(user.AvatarBlobPath, TimeSpan.FromHours(24))
                    : null,
                roles.AsReadOnly(),
                user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                user.IsDeleted,
                user.CreatedAt));
        }

        return Result.Ok(PaginatedResult<AdminUserDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount));
    }
}
