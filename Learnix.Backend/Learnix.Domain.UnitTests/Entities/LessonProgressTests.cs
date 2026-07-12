using Learnix.Domain.Entities;
using Learnix.Domain.Events.LessonProgress;

namespace Learnix.Domain.UnitTests.Entities;

public class LessonProgressTests
{
    private static LessonProgress Fresh()
        => LessonProgress.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_ShouldStartIncomplete()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        var progress = LessonProgress.Create(courseId, lessonId, studentId);

        // Assert
        progress.CourseId.Should().Be(courseId);
        progress.LessonId.Should().Be(lessonId);
        progress.StudentId.Should().Be(studentId);
        progress.IsCompleted.Should().BeFalse();
        progress.CompletedAt.Should().BeNull();
        progress.LastAccessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkCompleted_ShouldCompleteAndRaiseEvent()
    {
        // Arrange
        var progress = Fresh();

        // Act
        progress.MarkCompleted();

        // Assert
        progress.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var @event = progress.DomainEvents.OfType<LessonCompletedDomainEvent>()
            .Should().ContainSingle().Subject;
        @event.StudentId.Should().Be(progress.StudentId);
        @event.CourseId.Should().Be(progress.CourseId);
        @event.LessonId.Should().Be(progress.LessonId);
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyCompleted_ShouldNotRaiseASecondEvent()
    {
        // Arrange — this event drives course-completion evaluation; a re-mark must not re-trigger it
        var progress = Fresh();
        progress.MarkCompleted();
        var firstCompletedAt = progress.CompletedAt;

        // Act
        progress.MarkCompleted();

        // Assert
        progress.DomainEvents.OfType<LessonCompletedDomainEvent>().Should().ContainSingle();
        progress.CompletedAt.Should().Be(firstCompletedAt);
    }

    [Fact]
    public void Reset_ShouldClearCompletion()
    {
        // Arrange
        var progress = Fresh();
        progress.MarkCompleted();
        progress.ClearDomainEvents();

        // Act
        progress.Reset();

        // Assert
        progress.IsCompleted.Should().BeFalse();
        progress.CompletedAt.Should().BeNull();
        progress.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reset_ShouldAllowCompletionToBeRaisedAgain()
    {
        // Arrange — resetting is a genuine restart, so the next completion is a new event
        var progress = Fresh();
        progress.MarkCompleted();
        progress.ClearDomainEvents();
        progress.Reset();

        // Act
        progress.MarkCompleted();

        // Assert
        progress.DomainEvents.OfType<LessonCompletedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Touch_ShouldRefreshLastAccessedWithoutCompleting()
    {
        // Arrange
        var progress = Fresh();

        // Act
        progress.Touch();

        // Assert
        progress.IsCompleted.Should().BeFalse();
        progress.LastAccessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        progress.DomainEvents.Should().BeEmpty();
    }
}
