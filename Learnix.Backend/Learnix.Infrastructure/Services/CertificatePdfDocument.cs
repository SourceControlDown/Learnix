using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
    public static byte[] Generate(CertificateDocumentData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(60);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    // Brand header
                    col.Item()
                        .AlignCenter()
                        .Text("LEARNIX")
                        .FontSize(36)
                        .Bold()
                        .FontColor("#1a1a2e");

                    col.Item().Height(8);

                    col.Item()
                        .AlignCenter()
                        .Text("Certificate of Completion")
                        .FontSize(20)
                        .FontColor("#4a4a6a");

                    col.Item().Height(40);

                    // Divider
                    col.Item().LineHorizontal(1).LineColor("#d0d0e8");

                    col.Item().Height(30);

                    // Intro text
                    col.Item()
                        .AlignCenter()
                        .Text("This is to certify that")
                        .FontSize(13)
                        .FontColor("#6b6b8a");

                    col.Item().Height(14);

                    // Student name
                    col.Item()
                        .AlignCenter()
                        .Text(data.StudentFullName)
                        .FontSize(30)
                        .Bold()
                        .FontColor("#1a1a2e");

                    col.Item().Height(14);

                    col.Item()
                        .AlignCenter()
                        .Text("has successfully completed the course")
                        .FontSize(13)
                        .FontColor("#6b6b8a");

                    col.Item().Height(14);

                    // Course title
                    col.Item()
                        .AlignCenter()
                        .Text(data.CourseTitle)
                        .FontSize(22)
                        .SemiBold()
                        .FontColor("#1a1a2e");

                    col.Item().Height(30);

                    // Divider
                    col.Item().LineHorizontal(1).LineColor("#d0d0e8");

                    col.Item().Height(20);

                    // Footer row: instructor | date | code
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Instructor").FontSize(10).FontColor("#9090aa");
                            c.Item().Text(data.InstructorName).FontSize(12).SemiBold().FontColor("#1a1a2e");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Completed on").FontSize(10).FontColor("#9090aa");
                            c.Item().AlignCenter()
                                .Text(data.CompletedAt.ToString("MMMM d, yyyy"))
                                .FontSize(12).SemiBold().FontColor("#1a1a2e");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignRight().Text("Certificate Code").FontSize(10).FontColor("#9090aa");
                            c.Item().AlignRight().Text(data.Code).FontSize(11).FontColor("#1a1a2e");
                        });
                    });

                    col.Item().Height(12);

                    // Verification URL
                    col.Item()
                        .AlignCenter()
                        .Text($"Verify at: {data.VerificationUrl}")
                        .FontSize(9)
                        .FontColor("#aaaacc");
                });
            });
        }).GeneratePdf();
    }
}
