using FluentValidation;
using Learnix.Application.Auth.Validation;
using Learnix.Application.Common.Validation;

namespace Learnix.Application.Auth.Commands.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail();

        RuleFor(x => x.Password)
            .ValidPassword();

        RuleFor(x => x.FirstName)
            .ValidFirstName();

        RuleFor(x => x.LastName)
            .ValidLastName();
    }
}
