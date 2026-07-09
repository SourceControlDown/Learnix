using FluentValidation;
using Learnix.Application.AiChat.Constants;

namespace Learnix.Application.AiChat.Queries.GetMyLearningProfile;

internal sealed class GetMyLearningProfileValidator : AbstractValidator<GetMyLearningProfileQuery>
{
    public GetMyLearningProfileValidator()
    {
        RuleForEach(x => x.Sections)
            .Must(section => LearningProfileSections.All.Contains(section, StringComparer.OrdinalIgnoreCase))
            .WithMessage((_, section) => AiChatMessages.UnknownSection(section));
    }
}
