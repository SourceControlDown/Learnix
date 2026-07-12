using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Common.Validation;

public static class UserRules
{
    public static IRuleBuilderOptions<T, string> ValidFirstName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(UserConstants.FirstNameMaxLength);
    }

    public static IRuleBuilderOptions<T, string> ValidLastName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(UserConstants.LastNameMaxLength);
    }
}
