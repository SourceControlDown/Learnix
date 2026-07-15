using FluentValidation;
using Learnix.Application.Categories.Validation;

namespace Learnix.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .ApplyCategoryNameRules();

        RuleFor(x => x.Slug)
            .ApplyCategorySlugRules();

        RuleFor(x => x.ImageBlobPath)
            .NotEmpty()
            .When(x => x.ImageBlobPath is not null);
    }
}
