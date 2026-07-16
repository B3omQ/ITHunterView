using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace ITHunterview.Service.UseCase;

public class OptimizeUseCase : IOptimizeUseCase
{
    private readonly IOptimizeSessionRepository _sessionRepo;
    private readonly IServiceProvider _serviceProvider;

    public OptimizeUseCase(IOptimizeSessionRepository sessionRepo, IServiceProvider serviceProvider)
    {
        _sessionRepo = sessionRepo;
        _serviceProvider = serviceProvider;
    }

    public async Task<Guid> CreateSessionAsync(Guid matchId, Stream fileStream, string contentType)
    {
        // Route to correct extractor based on content type
        ICvExtractor extractor;
        string fileType;

        if (contentType.Contains("pdf"))
        {
            extractor = _serviceProvider.GetRequiredService<PdfCvExtractor>();
            fileType = "pdf";
        }
        else if (contentType.Contains("wordprocessingml"))
        {
            extractor = _serviceProvider.GetRequiredService<DocxCvExtractor>();
            fileType = "docx";
        }
        else
        {
            throw new ArgumentException("Unsupported file type.");
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

    public async Task<object> ApplySuggestionAsync(Guid sessionId, string suggestionId, string action, string? editedText)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session or CV Document not found");

        // In a real scenario, fetch the suggestion details from DB using suggestionId to get FieldPath.
        // Mock suggestion path and text
        string mockFieldPath = "Summary"; // e.g., suggestion.FieldPath
        string mockSuggestedText = "Optimized Summary"; // e.g., suggestion.SuggestedText

        if (action.Equals("accept", StringComparison.OrdinalIgnoreCase))
        {
            CvDocumentHelper.SetFieldByPath(session.CvDocument, mockFieldPath, mockSuggestedText);
            await _sessionRepo.UpdateAsync(session);
        }
        else if (action.Equals("edit", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(editedText)) throw new ArgumentException("Edited text is required for edit action");
            CvDocumentHelper.SetFieldByPath(session.CvDocument, mockFieldPath, editedText);
            await _sessionRepo.UpdateAsync(session);
        }
        else if (action.Equals("skip", StringComparison.OrdinalIgnoreCase))
        {
            // Do nothing to document, just mark skipped in DB (if tracking suggestion states)
        }

        // Trigger async preview generation (fire and forget)
        _ = Task.Run(async () => 
        {
            try 
            {
                using var scope = _serviceProvider.CreateScope();
                ICvRenderer renderer = session.OriginalFileType == "pdf" 
                    ? scope.ServiceProvider.GetRequiredService<PdfCvRenderer>() 
                    : scope.ServiceProvider.GetRequiredService<DocxCvRenderer>();
                
                using var previewStream = await renderer.RenderPreviewImageAsync(session.CvDocument);
                // Save stream to blob storage and notify via SignalR
                // e.g. await blobService.UploadAsync($"previews/{sessionId}.png", previewStream);
            }
            catch (Exception ex)
            {
                // Log exception
            }
        });

        // Mock new score logic: return the predefined scoreIfAccepted from the suggestion
        return new 
        { 
            NewScore = 85.5, 
            PreviewImageUrl = $"https://storage.local/previews/{sessionId}.png" // Placeholder
        };
    }

    public async Task<string> GenerateFinalFileAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session not found");

        ICvRenderer renderer = session.OriginalFileType == "pdf" 
            ? _serviceProvider.GetRequiredService<PdfCvRenderer>() 
            : _serviceProvider.GetRequiredService<DocxCvRenderer>();

        using var finalStream = await renderer.RenderFinalAsync(session.CvDocument);
        
        // Save stream to blob storage and return URL
        // e.g. return await blobService.UploadAsync($"optimized_cvs/{sessionId}.{session.OriginalFileType}", finalStream);
        
        return $"https://storage.local/optimized_cvs/{sessionId}.{session.OriginalFileType}";
    }
}
