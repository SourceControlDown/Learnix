using FluentValidation;
using Learnix.Application.InstructorAnalytics.Constants;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRecentReviews;

public sealed class GetInstructorRecentReviewsQueryValidator : AbstractValidator<GetInstructorRecentReviewsQuery>
{
    public GetInstructorRecentReviewsQueryValidator()
    {
        RuleFor(x => x.Take)
            .InclusiveBetween(InstructorAnalyticsConstants.MinRecentReviewsTake, InstructorAnalyticsConstants.MaxRecentReviewsTake);
    }
}
