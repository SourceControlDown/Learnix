using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.Certificates;

namespace Learnix.Domain.UnitTests.Entities;

public class CertificateTests
{
    private const string CourseTitle = "HTML & CSS Basics";
    private const string PdfPath = "certificates/CERT-20260711-ABCDEF12.pdf";

    private static Course NewCourse()
        => Course.Create(Guid.NewGuid(), Guid.NewGuid(), CourseTitle, "Desc", 0m);

    private static Enrollment CompletedEnrollmentFor(Course course)
    {
        var enrollment = Enrollment.Create(course.Id, Guid.NewGuid(), pricePaid: 0m);
        enrollment.MarkCompleted();
        return enrollment;
    }

    private static Certificate Issued()
    {
        var course = NewCourse();
        var certificate = Certificate.Issue(CompletedEnrollmentFor(course), course);
        certificate.ClearDomainEvents();
        return certificate;
    }

    // Issuing
    // =======
    [Fact]
    public void Issue_ShouldTakeEveryIdFromTheEnrollmentAndStartWithNoFile()
    {
        // Arrange
        var course = NewCourse();
        var enrollment = CompletedEnrollmentFor(course);

        // Act
        var certificate = Certificate.Issue(enrollment, course);

        // Assert — the PDF is rendered on demand, so a fresh certificate has no file yet
        certificate.CourseId.Should().Be(enrollment.CourseId);
        certificate.StudentId.Should().Be(enrollment.StudentId);
        certificate.EnrollmentId.Should().Be(enrollment.Id);
        certificate.FilePath.Should().BeNull();
        certificate.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Issue_ShouldGenerateItsOwnCode()
    {
        // Arrange
        var course = NewCourse();

        // Act — the code format belongs to the domain; no caller supplies it
        var certificate = Certificate.Issue(CompletedEnrollmentFor(course), course);

        // Assert
        certificate.Code.Should().StartWith($"CERT-{DateTime.UtcNow:yyyyMMdd}-");
        certificate.Code.Should().HaveLength(22);
    }

    [Fact]
    public void Issue_ShouldGenerateAUniqueCodePerCertificate()
    {
        // Arrange
        var course = NewCourse();

        // Act — the code is uniquely indexed and used for public verification
        var first = Certificate.Issue(CompletedEnrollmentFor(course), course);
        var second = Certificate.Issue(CompletedEnrollmentFor(course), course);

        // Assert
        first.Code.Should().NotBe(second.Code);
    }

    [Fact]
    public void Issue_ShouldRaiseIssuedEventNamingTheCourse()
    {
        // Arrange
        var course = NewCourse();
        var enrollment = CompletedEnrollmentFor(course);

        // Act
        var certificate = Certificate.Issue(enrollment, course);

        // Assert — the event is what notifies the student; it carries the title so nothing
        // downstream has to look the course up again
        var @event = certificate.DomainEvents.OfType<CertificateIssuedDomainEvent>()
            .Should().ContainSingle().Subject;
        @event.CertificateId.Should().Be(certificate.Id);
        @event.StudentId.Should().Be(enrollment.StudentId);
        @event.CourseId.Should().Be(course.Id);
        @event.CourseTitle.Should().Be(CourseTitle);
    }

    [Fact]
    public void Issue_WhenEnrollmentIsNotCompleted_ShouldThrowDomainException()
    {
        // Arrange — an active enrollment is a course still in progress
        var course = NewCourse();
        var enrollment = Enrollment.Create(course.Id, Guid.NewGuid(), pricePaid: 0m);

        // Act
        var act = () => Certificate.Issue(enrollment, course);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Certificate cannot be issued for an enrollment that is not completed.");
    }

    [Fact]
    public void Issue_WhenCourseIsNotTheEnrolledCourse_ShouldThrowDomainException()
    {
        // Arrange — passing a foreign course would name the wrong course in the event and hand the
        // student a certificate for something they never took
        var enrolledCourse = NewCourse();
        var otherCourse = NewCourse();
        var enrollment = CompletedEnrollmentFor(enrolledCourse);

        // Act
        var act = () => Certificate.Issue(enrollment, otherCourse);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Certificate course does not match the enrolled course.");
    }

    // Attaching the rendered PDF
    // =========================
    [Fact]
    public void AttachFile_ShouldStoreTheBlobPath()
    {
        // Arrange
        var certificate = Issued();

        // Act
        certificate.AttachFile(PdfPath);

        // Assert
        certificate.FilePath.Should().Be(PdfPath);
    }

    [Fact]
    public void AttachFile_WhenFirstAttached_ShouldNotRaiseAReplacedEvent()
    {
        // Arrange — there is no previous blob to reap
        var certificate = Issued();

        // Act
        certificate.AttachFile(PdfPath);

        // Assert
        certificate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AttachFile_WhenPathIsUnchanged_ShouldNotRaiseAReplacedEvent()
    {
        // Arrange — regenerating writes to the same deterministic path, overwriting in place.
        // A replaced event here would enqueue a delete for the file that was just uploaded.
        var certificate = Issued();
        certificate.AttachFile(PdfPath);

        // Act
        certificate.AttachFile(PdfPath);

        // Assert
        certificate.DomainEvents.Should().BeEmpty();
        certificate.FilePath.Should().Be(PdfPath);
    }

    [Fact]
    public void AttachFile_WhenPathChanges_ShouldRaiseReplacedEventCarryingTheOldPath()
    {
        // Arrange — only a genuinely different path orphans the previous blob
        var certificate = Issued();
        certificate.AttachFile(PdfPath);
        const string newPath = "certificates/CERT-20260711-99999999.pdf";

        // Act
        certificate.AttachFile(newPath);

        // Assert
        var @event = certificate.DomainEvents.OfType<CertificateFileReplacedDomainEvent>()
            .Should().ContainSingle().Subject;
        @event.CertificateId.Should().Be(certificate.Id);
        @event.PreviousFilePath.Should().Be(PdfPath);
        certificate.FilePath.Should().Be(newPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AttachFile_WhenPathIsBlank_ShouldThrowDomainException(string filePath)
    {
        // Arrange
        var certificate = Issued();

        // Act
        var act = () => certificate.AttachFile(filePath);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Certificate file path cannot be empty.");
        certificate.FilePath.Should().BeNull();
    }
}
