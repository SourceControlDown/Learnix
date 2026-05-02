using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Events;
using Learnix.Application.Common.Settings;
using Learnix.Domain.Events;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text;

namespace Learnix.Application.Auth.EventHandlers;

internal sealed class UserRegisteredDomainEventHandler(
    IEmailSender emailSender,
    IOptions<AppSettings> appSettings)
    : INotificationHandler<DomainEventNotification<UserRegisteredDomainEvent>>
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public async Task Handle(DomainEventNotification<UserRegisteredDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Identity tokens contain '+', '/', '=' — must be base64-url encoded for safe URL transport
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(domainEvent.EmailConfirmationToken));
        var link = $"{_appSettings.ClientBaseUrl}/verify-email?userId={domainEvent.UserId}&token={encodedToken}";

        await emailSender.SendEmailConfirmationAsync(domainEvent.Email, domainEvent.FirstName, link, cancellationToken);
    }
}