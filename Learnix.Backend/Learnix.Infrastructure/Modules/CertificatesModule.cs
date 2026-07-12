using Learnix.Application.Certificates.Abstractions;
using Learnix.Infrastructure.Services.Certificates;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Learnix.Infrastructure.Modules;

/// <summary>Certificate PDF generation (QuestPDF + QRCoder).</summary>
public static class CertificatesModule
{
    public static IServiceCollection AddCertificates(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        services.AddSingleton<ICertificatePdfGenerator, CertificatePdfGenerator>();

        return services;
    }
}
