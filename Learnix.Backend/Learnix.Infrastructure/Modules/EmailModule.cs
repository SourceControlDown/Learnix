using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>Email: SMTP transport plus RazorLight template rendering and its localization.</summary>
public static class EmailModule
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(ConfigurationSectionNameConstants.Smtp));

        services.AddLocalization();
        services.AddSingleton<EmailRenderer>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
