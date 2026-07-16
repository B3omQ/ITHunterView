using ITHunterview.Service.DTOs;

namespace ITHunterview.Service.Interface.UseCase;

public interface IOptimizeUseCase
{
    Task<Guid> CreateSessionAsync(Guid matchId, Stream fileStream, string contentType);
    Task<object> GetSuggestionsAsync(Guid sessionId);
    Task<object> ApplySuggestionAsync(Guid sessionId, string suggestionId, string action, string? editedText);
    Task<string> GenerateFinalFileAsync(Guid sessionId);
}
