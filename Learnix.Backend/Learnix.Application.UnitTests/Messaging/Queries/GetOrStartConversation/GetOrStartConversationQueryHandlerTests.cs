using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Queries.GetOrStartConversation;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Messaging.Queries.GetOrStartConversation;

public class GetOrStartConversationQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetOrStartConversationQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetOrStartConversationQueryHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(new User("instructor@learnix.dev", "Dev", "Instructor"));

        _sut = new GetOrStartConversationQueryHandler(
            _currentUser, _courseRepository, _enrollmentRepository, _conversationRepository,
            _userRepository, _unitOfWork);
    }

    private void Enrolled(Enrollment? enrollment) =>
        _enrollmentRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrollment);

    private void CourseIs(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private void ExistingConversation(CourseConversation? conversation) =>
        _conversationRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<CourseConversation>>(), Arg.Any<CancellationToken>())
            .Returns(conversation);

    private Task<FluentResults.Result<ConversationDto>> Act(Guid courseId) =>
        _sut.Handle(new GetOrStartConversationQuery(courseId), CancellationToken.None);

    [Fact]
    public async Task An_enrolled_student_with_no_thread_yet_gets_a_new_one()
    {
        // Arrange
        var course = CourseOf(InstructorId);
        CourseIs(course);
        Enrolled(Enrollment.Create(course.Id, StudentId, 0m));
        ExistingConversation(null);

        // Act
        var result = await Act(course.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OtherUserId.Should().Be(InstructorId);
        result.Value.OtherUserName.Should().Be("Dev Instructor");
        result.Value.UnreadCount.Should().Be(0);

        await _conversationRepository.Received(1)
            .AddAsync(Arg.Any<CourseConversation>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Opening the panel twice must not fork the thread — the second call returns the same conversation,
    /// unread count and all.
    /// </summary>
    [Fact]
    public async Task An_existing_thread_is_returned_rather_than_a_second_one_created()
    {
        // Arrange
        var course = CourseOf(InstructorId);
        var conversation = CourseConversation.Create(course.Id, StudentId, InstructorId);
        conversation.AddMessage(InstructorId, "Welcome aboard");

        CourseIs(course);
        Enrolled(Enrollment.Create(course.Id, StudentId, 0m));
        ExistingConversation(conversation);

        // Act
        var result = await Act(course.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(conversation.Id);
        result.Value.UnreadCount.Should().Be(1);

        await _conversationRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    /// <summary>The instructor's inbox is not a contact form: only a student of the course may open a thread.</summary>
    [Fact]
    public async Task A_stranger_to_the_course_cannot_open_a_thread_with_its_instructor()
    {
        // Arrange
        Enrolled(null);

        // Act
        var result = await Act(Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _conversationRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    private static Course CourseOf(Guid instructorId) =>
        Course.Create(instructorId, Guid.NewGuid(), "React", "…", 0m);
}
