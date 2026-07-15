using Learnix.Domain.Entities;
using Learnix.Domain.Events.Category;

namespace Learnix.Domain.UnitTests.Entities;

public class CategoryTests
{
    private const string Image = "category-images/img.webp";

    [Fact]
    public void Create_ShouldProduceANonSystemCategory()
    {
        // Act
        var category = Category.Create("Programming", "programming");

        // Assert
        category.IsSystem.Should().BeFalse();
        category.CoursesCount.Should().Be(0);
        category.ImageBlobPath.Should().BeNull();
    }

    [Fact]
    public void CreateSystem_ShouldFlagTheCategoryAsSeeded()
    {
        // Arrange & Act — system categories are protected from deletion/renaming (ADR-007)
        var category = Category.CreateSystem("Programming", "programming");

        // Assert
        category.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void Rename_ShouldUpdateNameAndSlug()
    {
        // Arrange
        var category = Category.Create("Old", "old");

        // Act
        category.Rename("New", "new");

        // Assert
        category.Name.Should().Be("New");
        category.Slug.Should().Be("new");
    }

    // Image lifecycle
    // ===============
    [Fact]
    public void SetImage_WhenNoPreviousImage_ShouldOnlyRaiseTheSetEvent()
    {
        // Arrange
        var category = Category.Create("Programming", "programming");
        category.ClearDomainEvents();

        // Act
        category.SetImage(Image);

        // Assert — nothing to release, so no deletion must be queued
        category.ImageBlobPath.Should().Be(Image);
        category.DomainEvents.Should().ContainSingle()
                .Which.Should().BeOfType<CategoryImageSetDomainEvent>();
    }

    [Fact]
    public void SetImage_WhenReplacingExisting_ShouldReleaseTheOldBlob()
    {
        // Arrange
        var category = Category.Create("Programming", "programming");
        category.SetImage("category-images/old.webp");
        category.ClearDomainEvents();

        // Act
        category.SetImage("category-images/new.webp");

        // Assert
        category.DomainEvents.Should().ContainSingle(e => e is CategoryImageRemovedDomainEvent)
                .Which.As<CategoryImageRemovedDomainEvent>().ImageBlobPath.Should().Be("category-images/old.webp");
        category.ImageBlobPath.Should().Be("category-images/new.webp");
    }

    [Fact]
    public void SetImage_WhenValueIsUnchanged_ShouldRaiseNoEvents()
    {
        // Arrange — otherwise a no-op save would delete the very blob it is re-attaching
        var category = Category.Create("Programming", "programming");
        category.SetImage(Image);
        category.ClearDomainEvents();

        // Act
        category.SetImage(Image);

        // Assert
        category.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetImage_WhenPassedNull_ShouldClearPathAndReleaseBlob()
    {
        // Arrange
        var category = Category.Create("Programming", "programming");
        category.SetImage(Image);
        category.ClearDomainEvents();

        // Act
        category.SetImage(null);

        // Assert
        category.ImageBlobPath.Should().BeNull();
        category.DomainEvents.Should().ContainSingle(e => e is CategoryImageRemovedDomainEvent);
    }

    [Fact]
    public void SetImage_WhenPassedNullAndNoImage_ShouldBeNoOp()
    {
        // Arrange
        var category = Category.Create("Programming", "programming");
        category.ClearDomainEvents();

        // Act
        category.SetImage(null);

        // Assert
        category.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PrepareForDeletion_ShouldReleaseTheImageBlob()
    {
        // Arrange — deleting a category must not orphan its image in blob storage
        var category = Category.Create("Programming", "programming");
        category.SetImage(Image);
        category.ClearDomainEvents();

        // Act
        category.PrepareForDeletion();

        // Assert
        category.DomainEvents.Should().ContainSingle(e => e is CategoryImageRemovedDomainEvent);
    }

    [Fact]
    public void PrepareForDeletion_WhenCalledTwice_ShouldReleaseTheImageBlobOnce()
    {
        // Arrange — an aggregate root may prepare a child that PrepareForDeletionInterceptor then
        // sweeps again; the blob must not be queued for deletion twice
        var category = Category.Create("Programming", "programming");
        category.SetImage(Image);
        category.ClearDomainEvents();

        // Act
        category.PrepareForDeletion();
        category.PrepareForDeletion();

        // Assert
        category.DomainEvents.Should().ContainSingle(e => e is CategoryImageRemovedDomainEvent);
    }

    // Course counter
    // ==============
    [Fact]
    public void DecrementCoursesCount_WhenAlreadyZero_ShouldClampAtZero()
    {
        // Arrange — a double-decrement (retry, race) must never produce a negative count
        var category = Category.Create("Programming", "programming");

        // Act
        category.DecrementCoursesCount();

        // Assert
        category.CoursesCount.Should().Be(0);
    }

    [Fact]
    public void IncrementAndDecrementCoursesCount_ShouldBalance()
    {
        // Arrange
        var category = Category.Create("Programming", "programming");

        // Act
        category.IncrementCoursesCount();
        category.IncrementCoursesCount();
        category.DecrementCoursesCount();

        // Assert
        category.CoursesCount.Should().Be(1);
    }
}
