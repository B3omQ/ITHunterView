using System.Text;
using System.Text.Json;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Service;
using UglyToad.PdfPig;

namespace ITHunterview.Service.Service;

public class PdfCvExtractor : ICvExtractor
{
    private readonly IAiService _aiService;

    public PdfCvExtractor(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<CvDocument> ExtractAsync(Stream fileStream)
    {
        // 1. Extract text and logic layout from PDF
        var rawText = ExtractTextFromPdf(fileStream);

        // 2. Call LLM to map to structured JSON
        return await MapToCvDocumentAsync(rawText);
    }

    private string ExtractTextFromPdf(Stream stream)
    {
        var sb = new StringBuilder();
        
        using var pdfDocument = PdfDocument.Open(stream);
        foreach (var page in pdfDocument.GetPages())
        {
            // Simple extraction for now.
            // A more complex heuristic would analyze word.BoundingBox and word.FontName
            foreach (var word in page.GetWords())
            {
                sb.Append(word.Text).Append(' ');
            }
            sb.AppendLine();
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
                var json = await _aiService.GenerateTextAsync(prompt, featureCode: "CV_EXTRACTION");
                
                // Clean markdown code blocks if any
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
                // In a real implementation, we could append the exception message to the prompt for the retry.
            }
        }

        throw new Exception("Failed to extract CV document.");
    }
}
