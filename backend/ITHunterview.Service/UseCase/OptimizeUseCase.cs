using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace ITHunterview.Service.UseCase;

public class OptimizeUseCase : IOptimizeUseCase
{
    private readonly IOptimizeSessionRepository _sessionRepo;
    private readonly IServiceProvider _serviceProvider;

    private readonly IHttpClientFactory _httpClientFactory;

    public OptimizeUseCase(IOptimizeSessionRepository sessionRepo, IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
    {
        _sessionRepo = sessionRepo;
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Guid> CreateSessionAsync(Guid matchId, string? cvUrl, Guid? cvId)
    {
        if (string.IsNullOrWhiteSpace(cvUrl) && !cvId.HasValue)
            throw new ArgumentException("Either CvUrl or CvId must be provided.");

        string finalUrl = cvUrl ?? "";
        
        string? dbFileType = null;
        // If cvId is provided but no url, fetch it from DB
        if (string.IsNullOrWhiteSpace(finalUrl) && cvId.HasValue)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Infrastructure.Persistence.ITHunterviewContext>();
            var cv = await dbContext.Cvs.FindAsync(cvId.Value);
            if (cv == null || string.IsNullOrWhiteSpace(cv.FileUrl))
                throw new ArgumentException("CV not found or has no FileUrl.");
            finalUrl = cv.FileUrl;
            dbFileType = cv.FileType;
        }

        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(finalUrl);
        response.EnsureSuccessStatusCode();
        var fileStream = await response.Content.ReadAsStreamAsync();
        
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        // Route to correct extractor based on content type or URL extension
        ICvExtractor extractor;
        string fileType;

        if (contentType.Contains("pdf") || finalUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || (dbFileType != null && dbFileType.Contains("pdf", StringComparison.OrdinalIgnoreCase)))
        {
            extractor = _serviceProvider.GetRequiredService<ITHunterview.Service.Service.PdfCvExtractor>();
            fileType = "pdf";
        }
        else if (contentType.Contains("wordprocessingml") || finalUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || (dbFileType != null && dbFileType.Contains("word", StringComparison.OrdinalIgnoreCase)) || (dbFileType != null && dbFileType.Contains("docx", StringComparison.OrdinalIgnoreCase)))
        {
            extractor = _serviceProvider.GetRequiredService<ITHunterview.Service.Service.DocxCvExtractor>();
            fileType = "docx";
        }
        else
        {
            throw new ArgumentException($"Unsupported file type. ContentType: {contentType}, FinalUrl: {finalUrl}, DbFileType: {dbFileType}");
        }

        var cvDoc = await extractor.ExtractAsync(fileStream);

        var session = new OptimizeSession
        {
            MatchSessionId = matchId,
            OriginalFileType = fileType,
            CvDocument = cvDoc
        };

        await _sessionRepo.CreateAsync(session);
        return session.Id;
    }

    public async Task<object> GetSuggestionsAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null) throw new KeyNotFoundException("Session not found");

        // In a real scenario, this fetches the matching result from the database (e.g. CvJobMatchScores)
        // using session.MatchSessionId to return the improvements list.
        // For now, we return a mock structure.
        return new { Suggestions = new List<object>() };
    }

    public async Task<object> ApplySuggestionAsync(Guid sessionId, string suggestionId, string action, string? editedText, string? originalText, string? suggestedText)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session or CV Document not found");

        if (action.Equals("accept", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(originalText) || string.IsNullOrEmpty(suggestedText)) 
                throw new ArgumentException("OriginalText and SuggestedText are required for accept action");
            CvDocumentHelper.ReplaceTextInDocument(session.CvDocument, originalText, suggestedText);
            await _sessionRepo.UpdateAsync(session);
        }
        else if (action.Equals("edit", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(originalText) || string.IsNullOrEmpty(editedText)) 
                throw new ArgumentException("OriginalText and EditedText are required for edit action");
            CvDocumentHelper.ReplaceTextInDocument(session.CvDocument, originalText, editedText);
            await _sessionRepo.UpdateAsync(session);
        }
        else if (action.Equals("skip", StringComparison.OrdinalIgnoreCase))
        {
            // Do nothing to document, just mark skipped in DB (if tracking suggestion states)
        }

        // We no longer trigger background preview generation here, 
        // because the frontend will call GeneratePreviewAsync on demand.

        // Mock new score logic: return the predefined scoreIfAccepted from the suggestion
        return new 
        { 
            NewScore = 85.5
        };
    }

    public async Task<string?> GeneratePreviewAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session not found");

        if (session.OriginalFileType == "pdf")
        {
            var renderer = _serviceProvider.GetRequiredService<ITHunterview.Service.Service.PdfCvRenderer>();
            using var previewStream = await renderer.RenderPreviewImageAsync(session.CvDocument);
            using var memoryStream = new MemoryStream();
            await previewStream.CopyToAsync(memoryStream);
            
            return $"data:image/png;base64,{Convert.ToBase64String(memoryStream.ToArray())}";
        }
        
        // For DOCX, we cannot easily generate an image without LibreOffice.
        // Return null or a placeholder to let the frontend know.
        return null;
    }

    public async Task<string> GenerateFinalFileAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session not found");

        ICvRenderer renderer = session.OriginalFileType == "pdf" 
            ? _serviceProvider.GetRequiredService<ITHunterview.Service.Service.PdfCvRenderer>() 
            : _serviceProvider.GetRequiredService<ITHunterview.Service.Service.DocxCvRenderer>();

        using var finalStream = await renderer.RenderFinalAsync(session.CvDocument);
        
        // Save stream to blob storage and return URL
        // e.g. return await blobService.UploadAsync($"optimized_cvs/{sessionId}.{session.OriginalFileType}", finalStream);
        
        return $"https://storage.local/optimized_cvs/{sessionId}.{session.OriginalFileType}";
    }
}
