using FluentResults;
using Learnix.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Learnix.Application.Auth.Commands.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(IIdentityService identityService)
    : IRequestHandler<ConfirmEmailCommand, Result>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        // Reverse base64-url encoding from email link
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        return await identityService.ConfirmEmailAsync(request.UserId, decodedToken, cancellationToken);
    }
}