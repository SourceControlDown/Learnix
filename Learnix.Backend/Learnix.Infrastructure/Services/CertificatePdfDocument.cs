using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System;

namespace Learnix.Infrastructure.Services;

internal sealed record CertificateDocumentData(
    string StudentFullName,
    string CourseTitle,
    string InstructorName,
    DateTime CompletedAt,
    string Code,
    string VerificationUrl);

internal static class CertificatePdfDocument
{
    private static byte[] GenerateQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(10);
    }

    public static byte[] Generate(CertificateDocumentData data)
    {
        var primaryColor = "#1e3a8a"; // Deep Blue
        var accentColor = "#d97706"; // Gold/Amber
        var textColor = "#1e293b"; // Slate-800
        var mutedColor = "#64748b"; // Slate-500

        byte[] qrCodeBytes = GenerateQrCode(data.VerificationUrl);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0);
                page.PageColor(Colors.White);
                
                page.DefaultTextStyle(x => x.FontFamily("Georgia").FontColor(textColor));

                // Use nested containers to create the border effect
                page.Content()
                    .Padding(20)
                    .Border(12)
                    .BorderColor(primaryColor)
                    .Padding(4)
                    .Border(1)
                    .BorderColor(accentColor)
                    .Padding(40)
                    .Column(col =>
                    {
                        col.Spacing(0);

                        // Brand header
                        col.Item()
                            .AlignCenter()
                            .Text("LEARNIX")
                            .FontSize(36)
                            .Bold()
                            .FontColor(primaryColor)
                            .LetterSpacing(0.2f);

                        col.Item().Height(4);

                        col.Item()
                            .AlignCenter()
                            .Text("Certificate of Completion")
                            .FontSize(24)
                            .FontColor(accentColor)
                            .Italic();

                        col.Item().Height(35);

                        // Intro text
                        col.Item()
                            .AlignCenter()
                            .Text("This is to certify that")
                            .FontSize(14)
                            .FontColor(mutedColor)
                            .Italic();

                        col.Item().Height(15);

                        // Student name
                        col.Item()
                            .AlignCenter()
                            .Text(data.StudentFullName)
                            .FontSize(38)
                            .Bold()
                            .FontColor(textColor);

                        col.Item().Height(15);

                        col.Item()
                            .AlignCenter()
                            .Text("has successfully completed the course")
                            .FontSize(14)
                            .FontColor(mutedColor)
                            .Italic();

                        col.Item().Height(15);

                        // Course title
                        col.Item()
                            .AlignCenter()
                            .Text(data.CourseTitle)
                            .FontSize(26)
                            .SemiBold()
                            .FontColor(primaryColor);

                        // Push footer to bottom
                        col.Item().ExtendVertical();

                        // Footer row
                        col.Item().Row(row =>
                        {
                            // Left: Instructor Signature
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(5).Text(data.InstructorName).FontSize(24).Italic().FontColor(primaryColor);
                                c.Item().LineHorizontal(1).LineColor(mutedColor);
                                c.Item().PaddingTop(4).Text("Instructor").FontSize(10).FontColor(mutedColor);
                                c.Item().Text(data.InstructorName).FontSize(12).SemiBold().FontColor(textColor);
                            });

                            // Center: Date & Code
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Awarded on").FontSize(10).FontColor(mutedColor);
                                c.Item().AlignCenter()
                                    .Text(data.CompletedAt.ToString("MMMM d, yyyy"))
                                    .FontSize(14).SemiBold().FontColor(textColor);
                                
                                c.Item().Height(10);
                                
                                c.Item().AlignCenter().Text("Certificate ID").FontSize(10).FontColor(mutedColor);
                                c.Item().AlignCenter().Text(data.Code).FontSize(10).FontColor(textColor);
                            });

                            // Right: QR Code & Verify
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Width(60).Height(60).Image(qrCodeBytes);
                                c.Item().Height(4);
                                c.Item().AlignRight().Text("Scan to verify").FontSize(9).FontColor(mutedColor);
                            });
                        });
                    });
            });
        }).GeneratePdf();
    }
}
