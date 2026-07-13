using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace LMS.BLL.Services
{
    public class CertificatePdfGenerator : ICertificatePdfGenerator
    {
        private readonly IWebHostEnvironment _env;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public CertificatePdfGenerator(IWebHostEnvironment env, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        private const string Ink = "#0f172a";
        private const string Accent = "#4f46e5";
        private const string AccentSoft = "#e0e7ff";
        private const string Muted = "#64748b";
        private const string Line = "#e2e8f0";

        public byte[] GenerateCertificatePdf(
            string studentName,
            string courseName,
            string instructorName,
            string dateIssued,
            string certificateId,
            int nameChangesCount
           ) 
        {
            string platformName = "LearningHub";
            var frontendUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:4200";
            var verificationUrl = $"{frontendUrl}/verify-certificate/{certificateId}";

            byte[]? qrCodeBytes = null;
            try
            {
                using (var qrGenerator = new QRCoder.QRCodeGenerator())
                {
                    using (var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCoder.QRCodeGenerator.ECCLevel.Q))
                    {
                        using (var pngByteQRCode = new QRCoder.PngByteQRCode(qrCodeData))
                        {
                            qrCodeBytes = pngByteQRCode.GetGraphic(20);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback silently if QR generation fails
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.Calibri).FontColor(Ink));

                    // Use page Background for the watermark
                    page.Background().AlignMiddle().AlignCenter()
                        .Text("CERTIFIED")
                        .FontSize(140).Bold().FontColor(Colors.Grey.Lighten4);

                    page.Header().Column(header =>
                    {
                        header.Spacing(0);
                        header.Item().Height(6).Background(Accent);

                        // ---- Platform branding header ----
                        header.Item().PaddingTop(12).Row(row =>
                        {
                            row.RelativeItem().Text(platformName)
                                .FontSize(14).Bold().FontColor(Accent).LetterSpacing(0.1f);

                            row.AutoItem().Text($"Ref: {certificateId}")
                                .FontSize(10).FontColor(Muted);
                        });

                        header.Item().PaddingTop(2).LineHorizontal(0.75f).LineColor(Line);
                    });

                    page.Content().AlignMiddle().Column(content =>
                    {
                        content.Spacing(0);

                        content.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                            .FontSize(15).LetterSpacing(0.25f).Bold().FontColor(Accent);

                        if (nameChangesCount > 0)
                        {
                            content.Item().PaddingTop(2).AlignCenter()
                                .Text($"REVISION {nameChangesCount + 1}")
                                .FontSize(9).LetterSpacing(0.15f).FontColor(Colors.Grey.Medium);
                        }

                        content.Item().PaddingTop(12).AlignCenter().Text("This is proudly presented to")
                            .FontSize(13).Italic().FontColor(Muted);

                        content.Item().PaddingTop(8).AlignCenter().Text(studentName)
                            .FontSize(38).Bold().FontColor(Ink);

                        content.Item().PaddingTop(6).AlignCenter().Width(180).Height(2).Background(Accent);

                        content.Item().PaddingTop(12).AlignCenter().Text("for successfully completing")
                            .FontSize(13).Italic().FontColor(Muted);

                        content.Item().PaddingTop(6).AlignCenter().Text(courseName)
                            .FontSize(23).SemiBold().FontColor(Accent);
                    });

                    page.PageColor(Colors.White);

                    page.Footer().Column(footer =>
                    {
                        footer.Spacing(0);

                        // ---- Data summary strip ----
                        footer.Item().PaddingBottom(12).Background(Colors.Grey.Lighten5)
                            .Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("INSTRUCTOR").FontSize(8).LetterSpacing(0.1f).FontColor(Muted);
                                col.Item().PaddingTop(2).Text(instructorName).FontSize(10).Bold();
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("DATE ISSUED").FontSize(8).LetterSpacing(0.1f).FontColor(Muted);
                                col.Item().PaddingTop(2).Text(dateIssued).FontSize(10).Bold();
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CERTIFICATE ID").FontSize(8).LetterSpacing(0.1f).FontColor(Muted);
                                col.Item().PaddingTop(2).Text(certificateId).FontSize(8).Bold();
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ISSUED BY").FontSize(8).LetterSpacing(0.1f).FontColor(Muted);
                                col.Item().PaddingTop(2).Text(platformName).FontSize(10).Bold();
                            });
                        });

                        // ---- Signature row ----
                        footer.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Width(160).BorderBottom(1).BorderColor(Line)
                                    .PaddingBottom(4).Text(instructorName).FontSize(13).SemiBold();
                                col.Item().PaddingTop(3).Text("Signature — Instructor")
                                    .FontSize(9).FontColor(Muted);
                            });

                            row.RelativeItem().AlignCenter().Column(col =>
                            {
                                if (qrCodeBytes != null)
                                {
                                    col.Item().AlignCenter().Width(52).Height(52)
                                        .Image(qrCodeBytes);
                                    col.Item().PaddingTop(2).AlignCenter()
                                        .Text("Scan to Verify").FontSize(6).FontColor(Muted).Bold();
                                }
                                else
                                {
                                    col.Item().AlignCenter().Width(56).Height(56)
                                        .Background(AccentSoft).Border(2).BorderColor(Accent)
                                        .AlignMiddle().AlignCenter()
                                        .Text("★").FontSize(22).FontColor(Accent);
                                }
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Width(160).BorderBottom(1).BorderColor(Line)
                                    .PaddingBottom(4).AlignRight().Text(dateIssued).FontSize(13).SemiBold();
                                col.Item().PaddingTop(3).AlignRight().Text("Signature — Date")
                                    .FontSize(9).FontColor(Muted);
                            });
                        });

                        footer.Item().AlignCenter().PaddingBottom(8)
                            .Text(text => {
                                text.Span("Verify authenticity at: ").FontSize(8).FontColor(Muted);
                                text.Span(verificationUrl).FontSize(8).FontColor(Accent).Underline();
                            });

                        footer.Item().Height(6).Background(Accent);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}