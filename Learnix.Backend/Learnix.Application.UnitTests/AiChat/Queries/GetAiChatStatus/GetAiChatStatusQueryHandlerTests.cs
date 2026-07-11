using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetAiChatStatus;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetAiChatStatus;

public class GetAiChatStatusQueryHandlerTests
{
    private readonly IAiChatProvider _provider = Substitute.For<IAiChatProvider>();
    private readonly IAiAvailabilityStore _availability = Substitute.For<IAiAvailabilityStore>();
    private readonly GetAiChatStatusQueryHandler _sut;

    public GetAiChatStatusQueryHandlerTests()
    {
        _provider.Name.Returns("Gemini");
        _provider.IsConfigured.Returns(true);
        _sut = new GetAiChatStatusQueryHandler(_provider, _availability);
    }

    private void Outage(AiOutage? outage) =>
        _availability.GetOutageAsync(Arg.Any<CancellationToken>()).Returns(outage);

    private Task<FluentResults.Result<AiChatStatusResponse>> Act() =>
        _sut.Handle(new GetAiChatStatusQuery(), CancellationToken.None);

    [Fact]
    public async Task Available_when_the_provider_is_configured_and_nothing_is_wrong()
    {
        // Arrange
        Outage(null);

        // Act
        var status = (await Act()).Value;

        // Assert
        status.Available.Should().BeTrue();
        status.Provider.Should().Be("Gemini");
        status.Reason.Should().BeNull();
    }

    [Fact]
    public async Task A_provider_without_an_api_key_is_down_but_does_not_say_so_to_the_student()
    {
        // Arrange
        _provider.IsConfigured.Returns(false);

        // Act
        var status = (await Act()).Value;

        // Assert
        status.Available.Should().BeFalse();
        status.RetryAtUtc.Should().BeNull();

        // The deployment is broken, which is not a fact the client is entitled to.
        status.Reason.Should().Be(AiOutageReasons.Unavailable);
        status.Reason.Should().NotBe(AiOutageReasons.NotConfigured);
        await _availability.DidNotReceiveWithAnyArgs().GetOutageAsync(default);
    }

    [Fact]
    public async Task An_outage_in_force_reports_its_reason_and_when_to_come_back()
    {
        // Arrange
        var retryAt = DateTime.UtcNow.AddMinutes(5);
        Outage(new AiOutage(AiOutageReasons.QuotaExceeded, "429 RESOURCE_EXHAUSTED", retryAt));

        // Act
        var status = (await Act()).Value;

        // Assert
        status.Available.Should().BeFalse();
        status.Reason.Should().Be(AiOutageReasons.QuotaExceeded);
        status.RetryAtUtc.Should().Be(retryAt);
    }

    [Fact]
    public async Task An_outage_whose_retry_time_has_passed_no_longer_locks_the_student_out()
    {
        // Arrange
        Outage(new AiOutage(
            AiOutageReasons.QuotaExceeded,
            "429",
            DateTime.UtcNow.AddSeconds(-1)));

        // Act
        var status = (await Act()).Value;

        // Assert
        status.Available.Should().BeTrue();
        status.Reason.Should().BeNull();
    }

    [Fact]
    public async Task A_rejected_key_blocks_the_chat_without_telling_the_client_the_key_was_rejected()
    {
        // Arrange
        Outage(new AiOutage(AiOutageReasons.Unauthorized, "401 invalid api key", RetryAtUtc: null));

        // Act
        var status = (await Act()).Value;

        // Assert
        status.Available.Should().BeFalse();
        status.Reason.Should().Be(AiOutageReasons.Unavailable);
        status.Reason.Should().NotBe(AiOutageReasons.Unauthorized);
    }
}
