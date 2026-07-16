using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Service;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ITHunterview.Service.Infrastructure.Service;

public class PdfCvRenderer : ICvRenderer
{
    public PdfCvRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<Stream> RenderFinalAsync(CvDocument doc)
    {
        var document = new CvPdfTemplate(doc);
        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }

    public Task<Stream> RenderPreviewImageAsync(CvDocument doc)
    {
        var document = new CvPdfTemplate(doc);
        var images = document.GenerateImages();
        var firstPage = images.First();
        var stream = new MemoryStream(firstPage);
        return Task.FromResult<Stream>(stream);
    }
}

public class CvPdfTemplate : IDocument
{
    private readonly CvDocument _doc;

    public CvPdfTemplate(CvDocument doc)
    {
        _doc = doc;
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
            });
    }

    void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(_doc.Header.FullName).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
            if (!string.IsNullOrEmpty(_doc.Header.Title))
                column.Item().Text(_doc.Header.Title).FontSize(14).FontColor(Colors.Grey.Darken2);

            var contactLine = string.Join(" | ", new[] { _doc.Header.Email, _doc.Header.Phone }.Where(s => !string.IsNullOrEmpty(s)));
            if (!string.IsNullOrEmpty(contactLine))
                column.Item().PaddingTop(5).Text(contactLine).FontSize(10);
            
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrEmpty(_doc.Summary))
            {
                column.Item().Text("Summary").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingBottom(10).Text(_doc.Summary);
            }

            if (_doc.Experience.Any())
            {
                column.Item().Text("Experience").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                foreach (var exp in _doc.Experience)
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"{exp.Role} at {exp.Company}").SemiBold();
                        if (!string.IsNullOrEmpty(exp.DateRange))
                            row.ConstantItem(100).AlignRight().Text(exp.DateRange).FontSize(10).FontColor(Colors.Grey.Darken2);
                    });

                    foreach (var bullet in exp.Bullets)
                    {
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(15).Text("•");
                            row.RelativeItem().Text(bullet);
                        });
                    }
                    column.Item().PaddingBottom(5);
                }
            }

            if (_doc.Education.Any())
            {
                column.Item().PaddingTop(10).Text("Education").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                foreach (var edu in _doc.Education)
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"{edu.Degree} - {edu.School}").SemiBold();
                        if (!string.IsNullOrEmpty(edu.DateRange))
                            row.ConstantItem(100).AlignRight().Text(edu.DateRange).FontSize(10).FontColor(Colors.Grey.Darken2);
                    });
                }
            }

            if (_doc.Skills.Any())
            {
                column.Item().PaddingTop(10).Text("Skills").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text(string.Join(", ", _doc.Skills));
            }
        });
    }
}
