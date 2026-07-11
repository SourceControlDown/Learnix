using Ardalis.Specification;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Services;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Enrollments.Services;

public class CourseCompletionServiceTests
{
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();

    private readonly CourseCompletionService _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid LessonA = Guid.NewGuid();
    private static readonly Guid LessonB = Guid.NewGuid();

    private readonly Course _course = Course.Create(
        Guid.NewGuid(), Guid.NewGuid(), "HTML & CSS Basics", "Desc", 0m);

    public CourseCompletionServiceTests()
    {
        _sut = new CourseCompletionService(
            _lessonRepository, _enrollmentRepository, _courseRepository, _certificateRepository);

        _courseRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_course);
    }

    [Fact]
    public async Task TryComplete_WhenTheLastLessonIsTheOneBeingCompletedNow_ShouldCompleteAndIssue()
    {
        // Arrange — LessonB is finishing in this very transaction, so its progress row may not be in
        // the database yet. This is the case the old `completedCount + 1` arithmetic got wrong.
        var enrollment = ActiveEnrollment();
        StubLessons((LessonA, true), (LessonB, false));
        StubEnrollment(enrollment);

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: LessonB);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        _certificateRepository.Received(1).Add(
            Arg.Is<Certificate>(c => c.CourseId == _course.Id && c.StudentId == StudentId));
    }

    [Fact]
    public async Task TryComplete_WhenTheCurrentLessonsRowWasAlreadyFlushed_ShouldStillCompleteExactlyOnce()
    {
        // Arrange — the same lesson counted both as flushed and as "just completed" must not be
        // double-counted; the union is idempotent
        var enrollment = ActiveEnrollment();
        StubLessons((LessonA, true), (LessonB, true));
        StubEnrollment(enrollment);

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: LessonB);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        _certificateRepository.Received(1).Add(Arg.Any<Certificate>());
    }

    [Fact]
    public async Task TryComplete_WhenAnotherLessonIsStillUnfinished_ShouldDoNothing()
    {
        // Arrange — LessonA remains; finishing LessonB is not the end of the course
        StubLessons((LessonA, false), (LessonB, false));

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: LessonB);

        // Assert
        _certificateRepository.DidNotReceive().Add(Arg.Any<Certificate>());
        await _enrollmentRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryComplete_WhenCourseHasNoVisibleLessons_ShouldDoNothing()
    {
        // Arrange — an empty course cannot be "finished", or every enrolled student would get a
        // certificate for learning nothing
        StubLessons();

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: null);

        // Assert
        _certificateRepository.DidNotReceive().Add(Arg.Any<Certificate>());
    }

    [Fact]
    public async Task TryComplete_WhenEnrollmentIsAlreadyCompleted_ShouldNotIssueASecondCertificate()
    {
        // Arrange — this is what makes the call safe to repeat
        var enrollment = ActiveEnrollment();
        enrollment.MarkCompleted();

        StubLessons((LessonA, true), (LessonB, true));
        StubEnrollment(enrollment);

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: null);

        // Assert
        _certificateRepository.DidNotReceive().Add(Arg.Any<Certificate>());
    }

    [Fact]
    public async Task TryComplete_WhenNotEnrolled_ShouldDoNothing()
    {
        // Arrange
        StubLessons((LessonA, true), (LessonB, true));
        StubEnrollment(null);

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: null);

        // Assert
        _certificateRepository.DidNotReceive().Add(Arg.Any<Certificate>());
    }

    [Fact]
    public async Task TryComplete_WithNoCurrentLesson_ShouldJudgeOnTheDatabaseAlone()
    {
        // Arrange — the self-healing path: the certificate endpoint asks "is this course finished?"
        // with no lesson in flight, and everything in the database says yes
        var enrollment = ActiveEnrollment();
        StubLessons((LessonA, true), (LessonB, true));
        StubEnrollment(enrollment);

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: null);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        _certificateRepository.Received(1).Add(Arg.Any<Certificate>());
    }

    [Fact]
    public async Task TryComplete_WithNoCurrentLesson_ShouldNotCompleteWhileALessonIsUnfinished()
    {
        // Arrange
        StubLessons((LessonA, true), (LessonB, false));

        // Act
        await _sut.TryCompleteAsync(StudentId, _course.Id, justCompletedLessonId: null);

        // Assert
        _certificateRepository.DidNotReceive().Add(Arg.Any<Certificate>());
    }

    // Fixtures

    private Enrollment ActiveEnrollment() =>
        Enrollment.Create(_course.Id, StudentId, pricePaid: 0m);

    private void StubLessons(params (Guid LessonId, bool IsCompleted)[] lessons) =>
        _lessonRepository
            .GetVisibleLessonCompletionAsync(StudentId, _course.Id, Arg.Any<CancellationToken>())
            .Returns(lessons.Select(l => new LessonCompletion(l.LessonId, l.IsCompleted)).ToList());

    private void StubEnrollment(Enrollment? enrollment) =>
        _enrollmentRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrollment);
}
