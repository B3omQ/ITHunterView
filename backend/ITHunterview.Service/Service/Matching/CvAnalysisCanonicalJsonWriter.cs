using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Service.Matching;

public static class CvAnalysisCanonicalJsonWriter
{
    private static readonly HashSet<string> ApplicationOwnedProperties = new(StringComparer.Ordinal)
    {
        "analysis_quality",
        "analysis_coverage",
        "analysis_diagnostics",
        "analysis_recovery"
    };

    private static readonly JsonSerializerOptions MetadataSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string Write(
        JsonElement providerRoot,
        CvAnalysisQuality quality,
        CvAnalysisCoverage coverage,
        IReadOnlyList<CvAnalysisDiagnostic> diagnostics,
        bool wasTruncated,
        CvAnalysisRecoveryMode? recoveryMode)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in providerRoot.EnumerateObject())
            {
                if (ApplicationOwnedProperties.Contains(property.Name))
                {
                    continue;
                }

                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }

            writer.WriteString("analysis_quality", quality.ToString());
            writer.WritePropertyName("analysis_coverage");
            JsonSerializer.Serialize(writer, coverage, MetadataSerializerOptions);
            writer.WritePropertyName("analysis_diagnostics");
            JsonSerializer.Serialize(writer, diagnostics, MetadataSerializerOptions);

            if (wasTruncated || recoveryMode is not null)
            {
                writer.WritePropertyName("analysis_recovery");
                writer.WriteStartObject();
                writer.WriteBoolean("was_truncated", wasTruncated);
                if (recoveryMode is not null)
                {
                    writer.WriteString("mode", recoveryMode.Value.ToString());
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
