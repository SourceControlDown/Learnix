using FluentValidation;
using Learnix.Application.AiChat.Constants;

namespace Learnix.Application.AiChat.Queries.GetCoursesByInstructor;

internal sealed class GetCoursesByInstructorValidator : AbstractValidator<GetCoursesByInstructorQuery>
{
    public GetCoursesByInstructorValidator()
    {
        RuleFor(x => x)
            .Must(x => x.InstructorId.HasValue || !string.IsNullOrWhiteSpace(x.InstructorName))
            .WithMessage(AiChatMessages.InstructorLookupRequired);

        RuleFor(x => x.InstructorName)
            .MaximumLength(AiChatToolLimits.InstructorNameMaxLength)
            .When(x => x.InstructorName is not null);
    }
}
