using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Wishlist.Abstractions;
using MediatR;

namespace Learnix.Application.Wishlist.Queries.GetWishlistCount;

public sealed class GetWishlistCountQueryHandler(
    ICurrentUserService currentUser,
    IWishlistRepository wishlistRepository)
    : IRequestHandler<GetWishlistCountQuery, Result<WishlistCountDto>>
{
    public async Task<Result<WishlistCountDto>> Handle(
        GetWishlistCountQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var count = await wishlistRepository.CountAsync(currentUser.UserId.Value, cancellationToken);

        return Result.Ok(new WishlistCountDto(count));
    }
}
