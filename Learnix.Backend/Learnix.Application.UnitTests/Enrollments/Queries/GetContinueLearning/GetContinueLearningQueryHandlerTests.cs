using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Queries.GetContinueLearning;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Enrollments.Queries.GetContinueLearning;

public class GetContinueLearningQueryHandlerTests
{
    private static readonly Guid StudentId = Guid.NewGuid();

    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonProgressRepository _lessonProgressRepository = Substitute.For<ILessonProgressRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();

    private readonly GetContinueLearningQueryHandler _sut;

    public GetContinueLearningQueryHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);

        _sut = new GetContinueLearningQueryHandler(
            _currentUser, _enrollmentRepository, _lessonProgressRepository, _lessonRepository);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(new GetContinueLearningQuery(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task Handle_WhenNoActiveEnrollments_ShouldReturnNull()
    {
        // Arrange
        StubEnrollments();

        // Act
        var result = await _sut.Handle(new GetContinueLearningQuery(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenSeveralCoursesHaveActivity_ShouldPickTheMostRecentlyActiveOne()
    {
        // Arrange — the stale course was enrolled last, so enrollment date must not decide here.
        var active = NewEnrollment("Recently studied", enrolledAt: new DateTime(2026, 1, 1));
        var stale = NewEnrollment("Abandoned", enrolledAt: new DateTime(2026, 6, 1));
        StubEnrollments(active, stale);

        StubLastActivity(new Dictionary<Guid, DateTime>
        {
            [active.CourseId] = new DateTime(2026, 7, 1),
            [stale.CourseId] = new DateTime(2026, 2, 1),
        });

        var resumeLessonId = Guid.NewGuid();
        StubResumeLesson(active.CourseId, resumeLessonId);

        // Act
        var result = await _sut.Handle(new GetContinueLearningQuery(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CourseId.Should().Be(active.CourseId);
        result.Value.CourseTitle.Should().Be("Recently studied");
        result.Value.LessonId.Should().Be(resumeLessonId);
    }

    [Fact]
    public async Task Handle_WhenNoCourseHasActivity_ShouldPickTheMostRecentlyEnrolledOne()
    {
        // Arrange
        var older = NewEnrollment("Older", enrolledAt: new DateTime(2026, 1, 1));
        var newer = NewEnrollment("Newer", enrolledAt: new DateTime(2026, 6, 1));
        StubEnrollments(older, newer);

        StubLastActivity(new Dictionary<Guid, DateTime>());

        var resumeLessonId = Guid.NewGuid();
        StubResumeLesson(newer.CourseId, resumeLessonId);

        // Act
        var result = await _sut.Handle(new GetContinueLearningQuery(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CourseId.Should().Be(newer.CourseId);
        result.Value.LessonId.Should().Be(resumeLessonId);
    }

    [Fact]
    public async Task Handle_WhenChosenCourseHasNoVisibleLessons_ShouldReturnNull()
    {
        // Arrange
        var enrollment = NewEnrollment("Empty course", enrolledAt: new DateTime(2026, 1, 1));
        StubEnrollments(enrollment);
        StubLastActivity(new Dictionary<Guid, DateTime>());

        _lessonRepository
            .GetResumeLessonIdAsync(StudentId, enrollment.CourseId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(new GetContinueLearningQuery(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    private static Enrollment NewEnrollment(string courseTitle, DateTime enrolledAt)
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), courseTitle, "Description", 0m);
        var enrollment = Enrollment.Create(course.Id, StudentId, 0m);

        // Enrollment.Course is populated by EF's Include; there is no domain method to set it.
        typeof(Enrollment)
            .GetProperty(nameof(Enrollment.Course))!
            .SetValue(enrollment, course);

        // EnrolledAt is stamped from the clock, so pin it to keep the ordering assertions deterministic.
        typeof(Enrollment)
            .GetProperty(nameof(Enrollment.EnrolledAt))!
            .SetValue(enrollment, enrolledAt);

        return enrollment;
    }

    private void StubEnrollments(params Enrollment[] enrollments) =>
        _enrollmentRepository
            .ListAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrollments.ToList());

    private void StubLastActivity(Dictionary<Guid, DateTime> activity) =>
        _lessonProgressRepository
            .GetLastActivityByCourseAsync(
                StudentId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, DateTime>)activity);

    private void StubResumeLesson(Guid courseId, Guid lessonId) =>
        _lessonRepository
            .GetResumeLessonIdAsync(StudentId, courseId, Arg.Any<CancellationToken>())
            .Returns((Guid?)lessonId);
}
