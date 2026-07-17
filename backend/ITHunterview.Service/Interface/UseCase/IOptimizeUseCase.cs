using ITHunterview.Service.DTOs;

namespace ITHunterview.Service.Interface.UseCase;

public interface IOptimizeUseCase
{
    Task<Guid> CreateSessionAsync(Guid matchId, string? cvUrl, Guid? cvId);
    Task<object> GetSuggestionsAsync(Guid sessionId);
    Task<object> ApplySuggestionAsync(Guid sessionId, string suggestionId, string action, string? editedText, string? originalText, string? suggestedText);
    Task<string?> GeneratePreviewAsync(Guid sessionId);
    Task<string> GenerateFinalFileAsync(Guid sessionId);
}
