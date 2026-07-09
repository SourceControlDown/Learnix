using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Queries.GetPublicCourses;

namespace Learnix.Application.UnitTests.Courses.Queries.GetPublicCourses;

public class GetPublicCoursesValidatorTests
{
    private readonly GetPublicCoursesValidator _validator = new();

    private static GetPublicCoursesQuery Query(string? search)
        => new(search, 0, 20, null, null, null, null, null);

    [Fact]
    public void Validate_WhenSearchIsAtMaxLength_ShouldPass()
    {
        // Arrange
        var search = new string('a', CourseValidationConstants.SearchMaxLength);

        // Act
        var result = _validator.Validate(Query(search));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenSearchExceedsMaxLength_ShouldFail()
    {
        // Arrange — the term is embedded in the Redis key on an anonymous endpoint,
        // so an unbounded term means an unbounded key space.
        var search = new string('a', CourseValidationConstants.SearchMaxLength + 1);

        // Act
        var result = _validator.Validate(Query(search));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetPublicCoursesQuery.Search));
    }

    [Fact]
    public void Validate_WhenSearchIsNull_ShouldPass()
    {
        // Act
        var result = _validator.Validate(Query(null));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
