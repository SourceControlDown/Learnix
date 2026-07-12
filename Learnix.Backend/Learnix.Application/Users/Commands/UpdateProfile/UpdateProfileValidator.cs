using FluentValidation;
using Learnix.Application.Common.Validation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .ValidFirstName();

        RuleFor(x => x.LastName)
            .ValidLastName();

        RuleFor(x => x.Bio)
            .MaximumLength(UserConstants.BioMaxLength)
            .When(x => x.Bio is not null);

        RuleFor(x => x.AvatarBlobPath)
            .MaximumLength(UserConstants.AvatarUrlMaxLength)
            .When(x => x.AvatarBlobPath is not null);
    }
}
