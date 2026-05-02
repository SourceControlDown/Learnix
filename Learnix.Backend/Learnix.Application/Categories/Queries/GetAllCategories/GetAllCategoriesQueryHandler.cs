using FluentResults;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAllCategories;

internal sealed class GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetAllCategoriesQuery, Result<IReadOnlyList<CategoryListItemDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryListItemDto>>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(new CategoriesOrderedSpecification(), cancellationToken);

        return Result.Ok<IReadOnlyList<CategoryListItemDto>>(
            categories
                .Select(c => new CategoryListItemDto(c.Id, c.Name, c.Slug))
                .ToList());
    }
}
