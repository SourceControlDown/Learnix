using Ardalis.Specification;
using Learnix.Application.AiChat.Queries.GetCoursesByInstructor;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetCoursesByInstructor;

public class GetCoursesByInstructorQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserRoleService _roleService = Substitute.For<IUserRoleService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private readonly GetCoursesByInstructorQueryHandler _sut;

    public GetCoursesByInstructorQueryHandlerTests()
    {
        _sut = new GetCoursesByInstructorQueryHandler(
            _userRepository, _roleService, _courseRepository, _categoryRepository);
    }

    [Fact]
    public async Task Handle_WhenNoUserMatchesTheName_ShouldReturnNotFound()
    {
        // Arrange
        _userRepository
            .ListAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.Handle(new GetCoursesByInstructorQuery("Ghost"), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenMatchedUserIsNotAnInstructor_ShouldReturnNotFound()
    {
        // Arrange
        var student = NewUser("Olena", "Kovalchuk");
        _userRepository
            .ListAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns([student]);
        RolesFor((student.Id, [Roles.Student]));

        // Act
        var result = await _sut.Handle(new GetCoursesByInstructorQuery("Olena"), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        await _courseRepository.DidNotReceive()
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSeveralInstructorsMatchTheName_ShouldReturnAmbiguousMatchesWithoutCourses()
    {
        // Arrange
        var first = NewUser("Olena", "Kovalchuk");
        var second = NewUser("Olena", "Marchenko");
        _userRepository
            .ListAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);
        RolesFor((first.Id, [Roles.Instructor]), (second.Id, [Roles.Instructor]));

        // Act
        var result = await _sut.Handle(new GetCoursesByInstructorQuery("Olena"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Instructor.Should().BeNull();
        result.Value.Courses.Should().BeNull();
        result.Value.Ambiguous.Should().HaveCount(2);
        result.Value.Ambiguous!.Select(a => a.FullName)
            .Should().BeEquivalentTo("Olena Kovalchuk", "Olena Marchenko");

        await _courseRepository.DidNotReceive()
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExactlyOneInstructorMatches_ShouldReturnTheirPublishedCourses()
    {
        // Arrange
        var instructor = NewUser("Olena", "Kovalchuk");
        var student = NewUser("Olena", "Student");
        _userRepository
            .ListAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns([instructor, student]);
        RolesFor((instructor.Id, [Roles.Instructor]), (student.Id, [Roles.Student]));

        var category = Category.Create("Programming", "programming");
        var course = Course.Create(instructor.Id, category.Id, "Advanced C#", "Deep dive", 49m);

        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([course]);
        _categoryRepository
            .ListAsync(Arg.Any<ISpecification<Category>>(), Arg.Any<CancellationToken>())
            .Returns([category]);

        // Act
        var result = await _sut.Handle(new GetCoursesByInstructorQuery("Olena Kovalchuk"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ambiguous.Should().BeNull();
        result.Value.Instructor!.InstructorId.Should().Be(instructor.Id);
        result.Value.Instructor.FullName.Should().Be("Olena Kovalchuk");
        result.Value.Courses.Should().ContainSingle();
        result.Value.Courses![0].Title.Should().Be("Advanced C#");
        result.Value.Courses[0].CategoryName.Should().Be("Programming");
        result.Value.Courses[0].IsFree.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenInstructorIdIsGiven_ShouldLookUpByIdInsteadOfName()
    {
        // Arrange
        var instructor = NewUser("Olena", "Kovalchuk");
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(instructor);
        RolesFor((instructor.Id, [Roles.Instructor]));

        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.Handle(new GetCoursesByInstructorQuery(InstructorId: instructor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Instructor!.InstructorId.Should().Be(instructor.Id);
        await _userRepository.DidNotReceive()
            .ListAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>());
    }

    private static User NewUser(string firstName, string lastName) =>
        new($"{firstName}.{lastName}@learnix.test".ToLowerInvariant(), firstName, lastName);

    private void RolesFor(params (Guid UserId, string[] Roles)[] assignments)
    {
        var map = assignments.ToDictionary(
            a => a.UserId,
            a => (IReadOnlyList<string>)a.Roles);

        _roleService
            .GetRolesBulkAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(map);
    }
}
