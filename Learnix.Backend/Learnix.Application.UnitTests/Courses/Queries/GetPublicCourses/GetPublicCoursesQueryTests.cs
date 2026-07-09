using Learnix.Application.Courses.Queries.GetPublicCourses;

namespace Learnix.Application.UnitTests.Courses.Queries.GetPublicCourses;

public class GetPublicCoursesQueryTests
{
    private static GetPublicCoursesQuery Query(
        string? search = null,
        int skip = 0,
        int take = 20,
        Guid? categoryId = null)
        => new(search, skip, take, categoryId, null, null, null, null);

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(19)]
    public void CacheKey_WhenRawSkipResolvesToTheSamePage_ShouldBeIdentical(int skip)
    {
        // Arrange — PaginationRequest.FromOffset floors skip/take, so 0..19 with take=20 are all page 0
        var pageZero = Query(skip: 0, take: 20);
        var sut = Query(skip: skip, take: 20);

        // Act & Assert
        sut.CacheKey.Should().Be(pageZero.CacheKey);
    }

    [Fact]
    public void CacheKey_WhenSkipResolvesToADifferentPage_ShouldDiffer()
    {
        // Arrange
        var pageZero = Query(skip: 0, take: 20);
        var pageOne = Query(skip: 20, take: 20);

        // Act & Assert
        pageOne.CacheKey.Should().NotBe(pageZero.CacheKey);
    }

    [Theory]
    [InlineData("react")]
    [InlineData("React")]
    [InlineData("  REACT  ")]
    public void CacheKey_WhenSearchDiffersOnlyByCaseOrPadding_ShouldBeIdentical(string search)
    {
        // Arrange — the DB filter uses ILike and the relevance sort compares lower(title),
        // so these terms select and order identically and must share one cache entry.
        var canonical = Query(search: "react");
        var sut = Query(search: search);

        // Act & Assert
        sut.CacheKey.Should().Be(canonical.CacheKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CacheKey_WhenSearchIsBlank_ShouldMatchNoSearch(string search)
    {
        // Arrange — a blank term is not applied as a filter, so it must not fork the cache
        var noSearch = Query(search: null);
        var sut = Query(search: search);

        // Act & Assert
        sut.CacheKey.Should().Be(noSearch.CacheKey);
    }

    [Fact]
    public void CacheKey_WhenAFilterDiffers_ShouldDiffer()
    {
        // Arrange
        var unfiltered = Query();
        var filtered = Query(categoryId: Guid.NewGuid());

        // Act & Assert
        filtered.CacheKey.Should().NotBe(unfiltered.CacheKey);
    }
}
