using Ardalis.Specification;
using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Commands.CreateVideoLesson;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Lessons.Commands.CreateVideoLesson;

public class CreateVideoLessonCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateVideoLessonCommandHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();
    private const string TempPath = "temp-uploads/raw.mp4";
    private const string CommittedPath = "course-videos/final.mp4";

    private readonly Course _course = Course.Create(InstructorId, Guid.NewGuid(), "React", "Learn React", 49m);
    private readonly Guid _sectionId;

    public CreateVideoLessonCommandHandlerTests()
    {
        _sut = new CreateVideoLessonCommandHandler(_courseRepository, _blobStorage, _unitOfWork, _currentUser);

        _sectionId = _course.AddSection("Section 1").Id;
        _currentUser.UserId.Returns(InstructorId);
        StubCourse(_course);
        StubCommit(Result.Ok(new BlobMetadata(CommittedPath, "video/mp4", 1024)));
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(Command(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
        await _blobStorage.DidNotReceiveWithAnyArgs().CommitUploadAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        StubCourse(null);

        var result = await _sut.Handle(Command(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenSectionDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _sut.Handle(Command(sectionId: Guid.NewGuid()), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    // Blob commit

    [Fact]
    public async Task Handle_WhenTheUploadCannotBeCommitted_ShouldFailAndAddNoLesson()
    {
        // Arrange — a temp blob that fails validation must not leave a lesson behind
        StubCommit(Result.Fail("invalid video"));

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        _course.Sections.Single().Lessons.Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Happy path

    [Fact]
    public async Task Handle_WhenValid_ShouldStoreTheCommittedPathAndReturnTheLessonId()
    {
        var result = await _sut.Handle(
            Command(title: "Intro", description: "What we cover", durationSeconds: 300), default);

        result.IsSuccess.Should().BeTrue();

        var lesson = _course.Sections.Single().Lessons.Should().ContainSingle().Subject;
        lesson.Should().BeOfType<VideoLesson>();
        lesson.Id.Should().Be(result.Value);
        lesson.LessonType.Should().Be(LessonType.Video);

        var video = lesson.As<VideoLesson>();
        video.Title.Should().Be("Intro");
        video.VideoBlobPath.Should().Be(CommittedPath);
        video.Description.Should().Be("What we cover");
        video.DurationSeconds.Should().Be(300);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCommitTheTempUploadItWasGiven()
    {
        await _sut.Handle(Command(), default);

        await _blobStorage.Received(1).CommitUploadAsync(TempPath, UploadTarget.LessonVideo, Arg.Any<CancellationToken>());
    }

    // Fixtures

    private CreateVideoLessonCommand Command(
        Guid? sectionId = null,
        string title = "Lesson",
        string? description = null,
        int? durationSeconds = null) =>
        new(_course.Id, sectionId ?? _sectionId, title, TempPath, description, durationSeconds);

    private void StubCourse(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private void StubCommit(Result<BlobMetadata> result) =>
        _blobStorage
            .CommitUploadAsync(Arg.Any<string>(), Arg.Any<UploadTarget>(), Arg.Any<CancellationToken>())
            .Returns(result);
}
