using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAllCategories;

public sealed record GetAllCategoriesQuery() : IRequest<Result<IReadOnlyList<CategoryListItemDto>>>;
