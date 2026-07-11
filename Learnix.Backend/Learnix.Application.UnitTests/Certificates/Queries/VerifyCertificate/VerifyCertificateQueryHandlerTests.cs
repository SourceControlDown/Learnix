using Ardalis.Specification;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Queries.VerifyCertificate;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Certificates.Queries.VerifyCertificate;

public class VerifyCertificateQueryHandlerTests
{
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly VerifyCertificateQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();

    public VerifyCertificateQueryHandlerTests()
    {
        _blobStorage.GenerateReadUrl(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns("https://blob/cert.pdf?sas");
        _sut = new VerifyCertificateQueryHandler(_certificateRepository, _userRepository, _blobStorage);
    }

    private void CertificateIs(Certificate? certificate) =>
        _certificateRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<Certificate>>(), Arg.Any<CancellationToken>())
            .Returns(certificate);

    private Task<FluentResults.Result<VerifyCertificateResponse>> Act(string code = "ABC-123") =>
        _sut.Handle(new VerifyCertificateQuery(code), CancellationToken.None);

    /// <summary>
    /// Verification is public — an employer holding the code has no account here. It must therefore say
    /// exactly enough to be worth trusting, and nothing more.
    /// </summary>
    [Fact]
    public async Task A_valid_code_names_the_course_the_student_and_the_instructor()
    {
        // Arrange
        var certificate = IssuedCertificate(out var course);
        certificate.AttachFile($"certificates/{certificate.Code}.pdf");
        CertificateIs(certificate);

        // Act
        var response = (await Act(certificate.Code)).Value;

        // Assert
        response.CourseTitle.Should().Be(course.Title);
        response.StudentFirstName.Should().Be("Dev");
        response.StudentLastName.Should().Be("Student");
        response.InstructorFirstName.Should().Be("Dev");
        response.IsReady.Should().BeTrue();
        response.DownloadUrl.Should().Be("https://blob/cert.pdf?sas");
    }

    /// <summary>
    /// The record exists before the PDF does. A verifier must be told the certificate is real but the file
    /// is not rendered yet, rather than handed a link to nothing.
    /// </summary>
    [Fact]
    public async Task A_certificate_whose_pdf_was_never_rendered_verifies_but_offers_no_download()
    {
        // Arrange
        CertificateIs(IssuedCertificate(out _));

        // Act
        var response = (await Act()).Value;

        // Assert
        response.IsReady.Should().BeFalse();
        response.DownloadUrl.Should().BeNull();
        _blobStorage.DidNotReceiveWithAnyArgs().GenerateReadUrl(default!, default);
    }

    [Fact]
    public async Task An_unknown_code_verifies_nothing()
    {
        // Arrange
        CertificateIs(null);

        // Act
        var result = await Act("MADE-UP");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    /// <summary>A deleted user must not turn a public verification page into a 500.</summary>
    [Fact]
    public async Task A_certificate_whose_student_is_gone_still_verifies()
    {
        // Arrange
        CertificateIs(IssuedCertificate(out _, withUsers: false));

        // Act
        var response = (await Act()).Value;

        // Assert
        response.StudentFirstName.Should().Be("Unknown");
        response.StudentLastName.Should().Be("Student");
        response.InstructorFirstName.Should().Be("Unknown");
    }

    private Certificate IssuedCertificate(out Course course, bool withUsers = true)
    {
        var instructorId = Guid.NewGuid();
        course = Course.Create(instructorId, Guid.NewGuid(), "React", "…", 0m);

        var enrollment = Enrollment.Create(course.Id, StudentId, 0m);
        enrollment.MarkCompleted();

        var certificate = Certificate.Issue(enrollment, course);

        // The query includes the course; the users are fetched separately, by id.
        typeof(Certificate).GetProperty(nameof(Certificate.Course))!.SetValue(certificate, course);

        if (withUsers)
        {
            _userRepository.GetByIdAsync(StudentId, Arg.Any<CancellationToken>())
                .Returns(new User("student@learnix.dev", "Dev", "Student"));
            _userRepository.GetByIdAsync(instructorId, Arg.Any<CancellationToken>())
                .Returns(new User("instructor@learnix.dev", "Dev", "Instructor"));
        }

        return certificate;
    }
}
