using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Commands.CreatePostLesson;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Lessons.Commands.CreatePostLesson;

public class CreatePostLessonCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreatePostLessonCommandHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    private readonly Course _course = Course.Create(InstructorId, Guid.NewGuid(), "React", "Learn React", 49m);
    private readonly Guid _sectionId;

    public CreatePostLessonCommandHandlerTests()
    {
        _sut = new CreatePostLessonCommandHandler(_courseRepository, _unitOfWork, _currentUser);

        // Default: the owning instructor acts on a draft course with one empty section.
        _sectionId = _course.AddSection("Section 1").Id;
        _currentUser.UserId.Returns(InstructorId);
        StubCourse(_course);
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(Command(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
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
    public async Task Handle_WhenCallerIsNotTheOwnerOrAdmin_ShouldReturnForbidden()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsInRole(Roles.Admin).Returns(false);

        var result = await _sut.Handle(Command(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSectionDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _sut.Handle(Command(sectionId: Guid.NewGuid()), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Happy path

    [Fact]
    public async Task Handle_WhenValid_ShouldAddThePostLessonAndReturnItsId()
    {
        var result = await _sut.Handle(Command(title: "Closures", content: "body"), default);

        result.IsSuccess.Should().BeTrue();

        var lesson = _course.Sections.Single().Lessons.Should().ContainSingle().Subject;
        lesson.Should().BeOfType<PostLesson>();
        lesson.Id.Should().Be(result.Value);
        lesson.Title.Should().Be("Closures");
        lesson.LessonType.Should().Be(LessonType.Post);
        lesson.As<PostLesson>().Content.Should().Be("body");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsAdmin_ShouldSucceed()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsInRole(Roles.Admin).Returns(true);

        var result = await _sut.Handle(Command(), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldAppendTheLessonAfterExistingOnes_UsingTheNextDisplayOrder()
    {
        // Arrange — the section already has a lesson at order 0
        _course.AddLesson(PostLesson.Create(_sectionId, "Existing", "content"));

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        var added = _course.Sections.Single().Lessons.Single(l => l.Id == result.Value);
        added.DisplayOrder.Should().Be(1);
    }

    // Fixtures

    private CreatePostLessonCommand Command(Guid? sectionId = null, string title = "Lesson", string content = "content") =>
        new(_course.Id, sectionId ?? _sectionId, title, content);

    private void StubCourse(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);
}
