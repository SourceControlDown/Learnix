using FluentResults;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Users.Queries.GetUserProfile;

internal sealed class GetUserProfileQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserProfileQuery, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(request.UserId),
            cancellationToken);

        if (user is null)
            return Result.Fail<UserProfileResponse>(new NotFoundError("User not found."));

        return Result.Ok(new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Bio,
            user.AvatarBlobPath));
    }
}
