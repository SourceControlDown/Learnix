using Ardalis.Specification;
using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Commands.RefreshToken;
using Learnix.Application.Auth.Models;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RefreshTokenEntity = Learnix.Domain.Entities.RefreshToken;

namespace Learnix.Application.UnitTests.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests
{
    private readonly IUserAuthenticationService _authService = Substitute.For<IUserAuthenticationService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // NSubstitute cannot proxy ILogger<T> when T is internal, and nothing here asserts on logging.
    private readonly ILogger<RefreshTokenCommandHandler> _logger =
        NullLogger<RefreshTokenCommandHandler>.Instance;

    private readonly RefreshTokenCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private const string PresentedToken = "presented-plain-token";
    private const string PresentedHash = "presented-hash";

    private static readonly RefreshTokenCommand Command = new(PresentedToken);

    public RefreshTokenCommandHandlerTests()
    {
        _sut = new RefreshTokenCommandHandler(
            _authService, _tokenService, _refreshTokenRepository, _blobStorage, _unitOfWork, _logger);

        _tokenService.HashRefreshToken(PresentedToken).Returns(PresentedHash);
        _tokenService.GenerateAccessToken(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>())
            .Returns(new AccessTokenResult("new-access", DateTime.UtcNow.AddMinutes(15)));
        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenResult("new-plain", "new-hash", DateTime.UtcNow.AddDays(7)));

        _authService.GetAuthenticationInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(AuthInfo()));
    }

    // Rejection paths

    [Fact]
    public async Task Handle_WhenTokenIsUnknown_ShouldReturnAuthenticationError()
    {
        // Arrange
        StubPresentedToken(null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ShouldReturnAuthenticationErrorWithoutRotating()
    {
        // Arrange
        StubPresentedToken(Token(expiresAt: DateTime.UtcNow.AddSeconds(-1)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
        await _refreshTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheTokenIsHashedBeforeLookup_ShouldNeverQueryByThePlainValue()
    {
        // Arrange — refresh tokens are stored hashed; querying by the plain token would never match
        StubPresentedToken(Token());

        // Act
        await _sut.Handle(Command, default);

        // Assert
        _tokenService.Received(1).HashRefreshToken(PresentedToken);
    }

    [Fact]
    public async Task Handle_WhenTheUserCanNoLongerAuthenticate_ShouldPropagateThatFailure()
    {
        // Arrange — e.g. the account was locked out or deleted between refreshes
        StubPresentedToken(Token());
        _authService.GetAuthenticationInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserAuthenticationInfo>(new ForbiddenError("Account is locked.")));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenError>();
        await _refreshTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    // Replay-attack protection

    [Fact]
    public async Task Handle_WhenARevokedTokenIsPresentedAgain_ShouldRevokeEveryActiveSessionForThatUser()
    {
        // Arrange — a stolen token was already rotated once; presenting it again means someone holds a copy
        var replayed = Token();
        replayed.Revoke();

        var otherSessions = new[] { Token(), Token() };

        StubPresentedToken(replayed);
        _refreshTokenRepository
            .ListAsync(Arg.Any<ISpecification<RefreshTokenEntity>>(), Arg.Any<CancellationToken>())
            .Returns(otherSessions.ToList());

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
        otherSessions.Should().OnlyContain(t => t.IsRevoked);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenARevokedTokenIsPresentedAgain_ShouldNotIssueNewTokens()
    {
        // Arrange
        var replayed = Token();
        replayed.Revoke();
        StubPresentedToken(replayed);
        StubActiveTokens();

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsFailed.Should().BeTrue();
        _tokenService.DidNotReceive().GenerateRefreshToken();
        _tokenService.DidNotReceiveWithAnyArgs().GenerateAccessToken(default, default!, default!, default!, default!, default);
    }

    // Rotation

    [Fact]
    public async Task Handle_WhenTokenIsValid_ShouldRevokeTheOldTokenAndPersistANewOne()
    {
        // Arrange
        var presented = Token();
        StubPresentedToken(presented);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        presented.IsRevoked.Should().BeTrue("the presented token must not be reusable after rotation");

        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Is<RefreshTokenEntity>(t => t.UserId == UserId && t.TokenHash == "new-hash" && !t.IsRevoked),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_ShouldReturnThePlainRefreshTokenAndNeverItsHash()
    {
        // Arrange — the hash stays in the database; only the plain value goes back to the client
        StubPresentedToken(Token());

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.RefreshToken.Should().Be("new-plain");
        result.Value.AccessToken.Should().Be("new-access");
    }

    [Fact]
    public async Task Handle_WhenUserHasAnAvatar_ShouldReturnItsPublicUrl()
    {
        // Arrange
        StubPresentedToken(Token());
        _authService.GetAuthenticationInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(AuthInfo(avatarBlobPath: "avatars/abc")));
        _blobStorage.GetPublicUrl("avatars/abc").Returns("https://cdn/avatars/abc");

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.AvatarUrl.Should().Be("https://cdn/avatars/abc");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAvatar_ShouldReturnNullUrlWithoutCallingBlobStorage()
    {
        // Arrange
        StubPresentedToken(Token());

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.AvatarUrl.Should().BeNull();
        _blobStorage.DidNotReceive().GetPublicUrl(Arg.Any<string>());
    }

    // Fixtures

    private static UserAuthenticationInfo AuthInfo(string? avatarBlobPath = null) =>
        new(UserId, "student@learnix.test", "Ola", "Student", ["Student"], EmailConfirmed: true, avatarBlobPath);

    private static RefreshTokenEntity Token(DateTime? expiresAt = null) =>
        new(UserId, PresentedHash, expiresAt ?? DateTime.UtcNow.AddDays(7));

    private void StubPresentedToken(RefreshTokenEntity? token) =>
        _refreshTokenRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<RefreshTokenEntity>>(), Arg.Any<CancellationToken>())
            .Returns(token);

    private void StubActiveTokens(params RefreshTokenEntity[] tokens) =>
        _refreshTokenRepository
            .ListAsync(Arg.Any<ISpecification<RefreshTokenEntity>>(), Arg.Any<CancellationToken>())
            .Returns(tokens.ToList());
}
