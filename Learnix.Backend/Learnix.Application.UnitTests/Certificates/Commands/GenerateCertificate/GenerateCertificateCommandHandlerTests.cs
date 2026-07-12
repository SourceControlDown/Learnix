using Ardalis.Specification;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Commands.GenerateCertificate;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Options;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Learnix.Application.UnitTests.Certificates.Commands.GenerateCertificate;

public class GenerateCertificateCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICertificatePdfGenerator _pdfGenerator = Substitute.For<ICertificatePdfGenerator>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly ICourseCompletionService _courseCompletion = Substitute.For<ICourseCompletionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GenerateCertificateCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();
    private readonly Course _course = Course.Create(InstructorId, Guid.NewGuid(), "React", "…", 0m);

    public GenerateCertificateCommandHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _pdfGenerator.Generate(Arg.Any<CertificateDocumentData>()).Returns([1, 2, 3]);
        _blobStorage.GenerateReadUrl(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns("https://blob/cert.pdf?sas");

        _courseRepository.GetByIdAsync(_course.Id, Arg.Any<CancellationToken>()).Returns(_course);
        _userRepository.GetByIdAsync(StudentId, Arg.Any<CancellationToken>())
            .Returns(new User("student@learnix.dev", "Dev", "Student"));
        _userRepository.GetByIdAsync(InstructorId, Arg.Any<CancellationToken>())
            .Returns(new User("instructor@learnix.dev", "Dev", "Instructor"));

        _sut = new GenerateCertificateCommandHandler(
            _currentUser, _enrollmentRepository, _certificateRepository, _courseRepository, _userRepository,
            _pdfGenerator, _blobStorage, _courseCompletion, _unitOfWork,
            Options.Create(new AppOptions { ClientBaseUrl = "https://learnix.dev" }));
    }

    private void EnrollmentIs(Enrollment? enrollment) =>
        _enrollmentRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrollment);

    private void CertificateIs(Certificate? certificate) =>
        _certificateRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<Certificate>>(), Arg.Any<CancellationToken>())
            .Returns(certificate);

    private Task<FluentResults.Result<string>> Act() =>
        _sut.Handle(new GenerateCertificateCommand(_course.Id), CancellationToken.None);

    [Fact]
    public async Task A_completed_course_renders_a_pdf_and_hands_back_a_signed_url()
    {
        // Arrange
        var enrollment = CompletedEnrollment();
        EnrollmentIs(enrollment);
        var certificate = Certificate.Issue(enrollment, _course);
        CertificateIs(certificate);

        // Act
        var result = await Act();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://blob/cert.pdf?sas");

        // The blob path is deterministic — regenerating overwrites in place instead of orphaning a file.
        certificate.FilePath.Should().Be($"certificates/{certificate.Code}.pdf");

        await _blobStorage.Received(1).UploadAsync(
            $"certificates/{certificate.Code}.pdf",
            Arg.Any<Stream>(),
            "application/pdf",
            Arg.Any<CancellationToken>());

        _pdfGenerator.Received(1).Generate(Arg.Is<CertificateDocumentData>(d =>
            d.StudentFullName == "Dev Student"
            && d.InstructorName == "Dev Instructor"
            && d.CourseTitle == "React"
            && d.VerificationUrl == $"https://learnix.dev/verify/{certificate.Code}"));
    }

    /// <summary>
    /// The last lesson and the certificate request can race. Completion is re-checked here first, so the
    /// student who asks a moment too early still gets their certificate instead of a refusal.
    /// </summary>
    [Fact]
    public async Task Completion_is_re_checked_before_the_enrollment_is_read()
    {
        // Arrange
        var enrollment = CompletedEnrollment();
        EnrollmentIs(enrollment);
        CertificateIs(Certificate.Issue(enrollment, _course));

        // Act
        await Act();

        // Assert
        await _courseCompletion.Received(1).TryCompleteAsync(
            StudentId, _course.Id, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unfinished_course_yields_no_certificate()
    {
        // Arrange
        EnrollmentIs(Enrollment.Create(_course.Id, StudentId, 0m));

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        _pdfGenerator.DidNotReceiveWithAnyArgs().Generate(default!);
        await _blobStorage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task A_student_who_was_never_enrolled_gets_nothing()
    {
        // Arrange
        EnrollmentIs(null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
    }

    /// <summary>
    /// The record is written when the course is finished, not here. Its absence is a broken invariant,
    /// not something to paper over by minting a fresh code on the download path.
    /// </summary>
    [Fact]
    public async Task A_completed_course_without_a_certificate_record_is_not_found_not_reissued()
    {
        // Arrange
        EnrollmentIs(CompletedEnrollment());
        CertificateIs(null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        _pdfGenerator.DidNotReceiveWithAnyArgs().Generate(default!);
    }

    private Enrollment CompletedEnrollment()
    {
        var enrollment = Enrollment.Create(_course.Id, StudentId, 0m);
        enrollment.MarkCompleted();
        return enrollment;
    }
}
