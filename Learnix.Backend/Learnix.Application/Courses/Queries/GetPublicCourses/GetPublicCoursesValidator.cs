using FluentValidation;
using Learnix.Application.Common.Constants;
using Learnix.Application.Courses.Constants;
using Learnix.Domain.Constants;

namespace Learnix.Application.Courses.Queries.GetPublicCourses;

public sealed class GetPublicCoursesValidator : AbstractValidator<GetPublicCoursesQuery>
{
    private static readonly string[] AllowedSortValues = ["popular", "newest", "rating"];

    public GetPublicCoursesValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take)
            .InclusiveBetween(1, PaginationConstants.MaxPageSize);
        RuleFor(x => x.Search)
            .MaximumLength(CourseValidationConstants.SearchMaxLength)
            .When(x => x.Search is not null);
        RuleFor(x => x.SortBy)
            .Must(v => v is null || AllowedSortValues.Contains(v))
            .WithMessage($"sortBy must be one of: {string.Join(", ", AllowedSortValues)}");
        // The filter floor is 0 ("no minimum"), but its ceiling is the domain's rating scale — if the
        // scale ever changes, this must not be the place that still believes in 5.
        RuleFor(x => x.MinRating)
            .InclusiveBetween(0m, ReviewConstants.MaxRating)
            .When(x => x.MinRating.HasValue);
    }
}
