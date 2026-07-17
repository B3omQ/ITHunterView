using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Service;

namespace ITHunterview.Service.Service;

public class DocxCvExtractor : ICvExtractor
{
    private readonly IAiService _aiService;

    public DocxCvExtractor(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<CvDocument> ExtractAsync(Stream fileStream)
    {
        var rawText = ExtractTextFromDocx(fileStream);
        return await MapToCvDocumentAsync(rawText);
    }

    private string ExtractTextFromDocx(Stream stream)
    {
        var sb = new StringBuilder();

        using (var wordDocument = WordprocessingDocument.Open(stream, false))
        {
            var body = wordDocument.MainDocumentPart?.Document.Body;
            if (body == null) return string.Empty;

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                // Simple extraction: can enhance with style reading for LLM hints
                // e.g. if (paragraph.ParagraphProperties?.ParagraphStyleId?.Val != null) ...
                sb.AppendLine(paragraph.InnerText);
            }
        }

        return sb.ToString();
    }

    private async Task<CvDocument> MapToCvDocumentAsync(string rawText)
    {
        var prompt = $$"""
            You are a professional CV parser. 
            Parse the following resume text into a strict JSON object matching this schema exactly.
            Do not include Markdown formatting or any other text.
            Schema:
            {
                "Header": { "FullName": "str", "Title": "str", "Email": "str", "Phone": "str" },
                "Summary": "str",
                "Experience": [ { "Company": "str", "Role": "str", "DateRange": "str", "Bullets": ["str"] } ],
                "Skills": ["str"],
                "Education": [ { "School": "str", "Degree": "str", "DateRange": "str" } ]
            }
            
            Resume Text:
            {{rawText}}
            """;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var json = await _aiService.GenerateTextAsync(prompt);
                
                if (json != null && json.StartsWith("```json"))
                {
                    json = json.Substring(7);
                    json = json.TrimEnd('`').Trim();
                }
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<CvDocument>(json!, options);
                if (doc != null) return doc;
            }
            catch (JsonException ex)
            {
                if (attempt == 3) throw new Exception("Failed to map CV to structured JSON after 3 attempts.", ex);
            }
        }

        throw new Exception("Failed to extract CV document.");
    }
}
