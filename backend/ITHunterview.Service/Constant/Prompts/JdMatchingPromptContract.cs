using System;
using System.Text.Json;

namespace ITHunterview.Service.Constant.Prompts;

public static class JdMatchingPromptContract
{
    public const string PromptKey = BypassMatchingPrompt.Key;
    public const string ContractV3 = "jd-matching/v3";
    public const string CvPlaceholder = "[CV_TEXT]";
    public const string RequirementsPlaceholder = "[PARSED_JD_REQUIREMENTS]";

    public static bool IsV3(string? modelConfig)
    {
        if (string.IsNullOrWhiteSpace(modelConfig)) return false;
        try
        {
            using var document = JsonDocument.Parse(modelConfig);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("contract", out var contract) &&
                   string.Equals(contract.GetString(), ContractV3, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
