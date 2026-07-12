using FluentResults;
using MediatR;

namespace Learnix.Application.Wishlist.Queries.GetWishlistCount;

public sealed record GetWishlistCountQuery : IRequest<Result<WishlistCountDto>>;
