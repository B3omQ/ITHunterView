using System.Text.Json;

namespace ITHunterview.Service.Interface.UseCase;

public interface IJdAnalysisTestUseCase
{
    Task<JsonElement> AnalyzeAsync(string jdText, CancellationToken ct = default);
}
