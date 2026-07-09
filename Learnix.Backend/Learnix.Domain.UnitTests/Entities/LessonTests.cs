using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Lessons;

namespace Learnix.Domain.UnitTests.Entities;

public class VideoLessonTests
{
    private const string Video = "course-videos/v.mp4";

    private static VideoLesson Create(string blobPath = Video)
        => VideoLesson.Create(Guid.NewGuid(), "Lesson", 0, blobPath);

    [Fact]
    public void Create_ShouldStartHiddenAndAttachTheVideo()
    {
        // Act — unlike a post lesson, a video lesson is not auto-published on creation
        var lesson = Create();

        // Assert
        lesson.IsHidden.Should().BeTrue();
        lesson.LessonType.Should().Be(LessonType.Video);
        lesson.DomainEvents.Should().ContainSingle(e => e is LessonVideoSetDomainEvent);
    }

    [Fact]
    public void Create_WithoutVideo_ShouldRaiseNoAttachEvent()
    {
        // Act
        var lesson = Create(blobPath: "");

        // Assert
        lesson.DomainEvents.Should().BeEmpty();
        lesson.IsPublishReady().Should().BeFalse();
    }

    [Fact]
    public void IsPublishReady_ShouldRequireAVideo()
    {
        // Act & Assert
        Create().IsPublishReady().Should().BeTrue();
        Create(blobPath: "   ").IsPublishReady().Should().BeFalse();
    }

    [Fact]
    public void ReplaceVideo_ShouldReleaseTheOldBlobAndAttachTheNew()
    {
        // Arrange
        var lesson = Create("course-videos/old.mp4");
        lesson.ClearDomainEvents();

        // Act
        lesson.ReplaceVideo("course-videos/new.mp4");

        // Assert
        lesson.VideoBlobPath.Should().Be("course-videos/new.mp4");
        lesson.DomainEvents.Should().ContainSingle(e => e is LessonVideoReleasedDomainEvent)
              .Which.As<LessonVideoReleasedDomainEvent>().ReleasedBlobPath.Should().Be("course-videos/old.mp4");
        lesson.DomainEvents.Should().ContainSingle(e => e is LessonVideoSetDomainEvent);
    }

    [Fact]
    public void ReplaceVideo_WithTheSamePath_ShouldRaiseNoEvents()
    {
        // Arrange — re-submitting the same blob must not queue its own deletion
        var lesson = Create();
        lesson.ClearDomainEvents();

        // Act
        lesson.ReplaceVideo(Video);

        // Assert
        lesson.DomainEvents.Should().BeEmpty();
        lesson.VideoBlobPath.Should().Be(Video);
    }

    [Fact]
    public void PrepareForDeletion_ShouldReleaseTheVideoBlob()
    {
        // Arrange
        var lesson = Create();
        lesson.ClearDomainEvents();

        // Act
        lesson.PrepareForDeletion();

        // Assert
        lesson.DomainEvents.Should().ContainSingle(e => e is LessonVideoReleasedDomainEvent);
    }

    [Fact]
    public void PrepareForDeletion_WhenThereIsNoVideo_ShouldRaiseNoEvent()
    {
        // Arrange
        var lesson = Create(blobPath: "");
        lesson.ClearDomainEvents();

        // Act
        lesson.PrepareForDeletion();

        // Assert
        lesson.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetVisibility_WhenNotPublishReady_ShouldThrowDomainException()
    {
        // Arrange
        var lesson = Create(blobPath: "");

        // Act
        var act = () => lesson.SetVisibility(true);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Cannot make this lesson visible");
    }

    [Fact]
    public void SetVisibility_WhenPublishReady_ShouldReveal()
    {
        // Arrange
        var lesson = Create();

        // Act
        lesson.SetVisibility(true);

        // Assert
        lesson.IsHidden.Should().BeFalse();
    }
}

public class PostLessonTests
{
    private static PostLesson Create(string content = "some content")
        => PostLesson.Create(Guid.NewGuid(), "Lesson", 0, content);

    [Fact]
    public void Create_WithContent_ShouldBeVisibleImmediately()
    {
        // Act — a post lesson is complete the moment it has content, so it needs no manual reveal
        var lesson = Create();

        // Assert
        lesson.IsHidden.Should().BeFalse();
        lesson.LessonType.Should().Be(LessonType.Post);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankContent_ShouldStayHidden(string content)
    {
        // Act
        var lesson = Create(content);

        // Assert
        lesson.IsHidden.Should().BeTrue();
        lesson.IsPublishReady().Should().BeFalse();
    }

    [Fact]
    public void UpdatePost_WhenContentIsCleared_ShouldHideTheLesson()
    {
        // Arrange — content is what makes a post lesson publishable; losing it must hide it again
        var lesson = Create();

        // Act
        lesson.UpdatePost("Lesson", "");

        // Assert
        lesson.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void UpdatePost_WhenContentRemains_ShouldKeepTheLessonVisible()
    {
        // Arrange
        var lesson = Create();

        // Act
        lesson.UpdatePost("New title", "new content");

        // Assert
        lesson.Title.Should().Be("New title");
        lesson.Content.Should().Be("new content");
        lesson.IsHidden.Should().BeFalse();
    }

    [Fact]
    public void UpdatePost_ShouldNotRevealAnAlreadyHiddenLesson()
    {
        // Arrange — EvaluateVisibility only hides; it never un-hides an intentionally hidden lesson
        var lesson = Create();
        lesson.SetVisibility(false);

        // Act
        lesson.UpdatePost("Lesson", "still has content");

        // Assert
        lesson.IsHidden.Should().BeTrue();
    }
}
