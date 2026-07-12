using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Options;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>
/// Auth services: token issuing, Google ID-token validation, registration, password flows.
/// The authentication/authorization <em>pipeline</em> (JWT bearer validation, policies) is an HTTP
/// concern and lives in the API layer — see <c>Learnix.API/Extensions/AuthenticationExtensions.cs</c>.
/// </summary>
/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-005: JWT secret — placeholder in base + dev-secret in Development + env var in production
/// - ADR-BACK-AUTH-016: 6-Digit OTP for Email Confirmation instead of Magic Link
/// </remarks>
public static class AuthModule
{
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(ConfigurationSectionNameConstants.Jwt));
        services.Configure<GoogleOptions>(configuration.GetSection(ConfigurationSectionNameConstants.Google));

        var googleOptions = configuration.GetSection(ConfigurationSectionNameConstants.Google).Get<GoogleOptions>()
            ?? throw new InvalidOperationException(
                $"Missing '{ConfigurationSectionNameConstants.Google}' configuration section.");

        if (string.IsNullOrWhiteSpace(googleOptions.ClientId))
            throw new InvalidOperationException("Google OAuth Client ID is not configured.");

        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IChangePasswordService, ChangePasswordService>();
        services.AddScoped<ISetPasswordService, SetPasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRoleService, UserRoleService>();

        return services;
    }
}
