using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Service;

namespace ITHunterview.Service.Infrastructure.Service;

public class DocxCvRenderer : ICvRenderer
{
    private const string TemplatePath = "Infrastructure/Service/Templates/cv_template.docx";

    public async Task<Stream> RenderFinalAsync(CvDocument doc)
    {
        // 1. In a real scenario, read the base template from disk/blob
        // byte[] templateBytes = await File.ReadAllBytesAsync(TemplatePath);
        // For this implementation without a real template file, we'll create an empty doc
        var stream = new MemoryStream();
        
        using (var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body;

            // Header
            body.Append(new Paragraph(new Run(new Text(doc.Header.FullName) { Space = SpaceProcessingModeValues.Preserve })));
            if (!string.IsNullOrEmpty(doc.Header.Title))
                body.Append(new Paragraph(new Run(new Text(doc.Header.Title))));
            
            // Summary
            if (!string.IsNullOrEmpty(doc.Summary))
            {
                body.Append(new Paragraph(new Run(new Text("Summary"))));
                body.Append(new Paragraph(new Run(new Text(doc.Summary))));
            }

            // Experience
            if (doc.Experience.Any())
            {
                body.Append(new Paragraph(new Run(new Text("Experience"))));
                foreach (var exp in doc.Experience)
                {
                    body.Append(new Paragraph(new Run(new Text($"{exp.Role} - {exp.Company} ({exp.DateRange})"))));
                    foreach (var bullet in exp.Bullets)
                    {
                        body.Append(new Paragraph(new Run(new Text($"• {bullet}"))));
                    }
                }
            }

            // Real implementation would replace {{placeholders}} in an existing template
            // e.g. foreach(var text in body.Descendants<Text>().Where(t => t.Text.Contains("{{header.fullName}}"))) { text.Text = text.Text.Replace("{{header.fullName}}", doc.Header.FullName); }

            wordDoc.MainDocumentPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    public async Task<Stream> RenderPreviewImageAsync(CvDocument doc)
    {
        // Render the DOCX first
        var docxStream = await RenderFinalAsync(doc);

        /*
        > [!NOTE]
        > To convert DOCX to PDF/Image for preview, we need LibreOffice headless.
        > Example implementation via Process.Start:
        > 
        > string tempDocx = Path.GetTempFileName() + ".docx";
        > await File.WriteAllBytesAsync(tempDocx, ((MemoryStream)docxStream).ToArray());
        > 
        > var process = new Process
        > {
        >     StartInfo = new ProcessStartInfo
        >     {
        >         FileName = "soffice",
        >         Arguments = $"--headless --convert-to pdf {tempDocx} --outdir {Path.GetTempPath()}",
        >         UseShellExecute = false,
        >         RedirectStandardOutput = true,
        >         CreateNoWindow = true
        >     }
        > };
        > process.Start();
        > await process.WaitForExitAsync();
        > 
        > var pdfPath = Path.ChangeExtension(tempDocx, ".pdf");
        > var pdfStream = new MemoryStream(await File.ReadAllBytesAsync(pdfPath));
        > 
        > // Optionally convert the PDF page 1 to an image using ImageMagick or Ghostscript.
        > // For this demo, we will just return the DOCX stream or throw NotImplementedException 
        > // because the environment doesn't have LibreOffice installed.
        */

        // Returning the DOCX stream as a placeholder
        return docxStream;
    }
}
