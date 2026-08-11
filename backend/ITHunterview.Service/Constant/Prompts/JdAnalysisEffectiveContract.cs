using System;
using System.Collections.Generic;

namespace ITHunterview.Service.Constant.Prompts;

/// <summary>
/// Application-owned persisted JD analysis contract. This contract is
/// intentionally independent from prompt-pair compatibility metadata and the
/// provider-facing output schema.
/// </summary>
public static class JdAnalysisEffectiveContract
{
    public const string SchemaVersion = "jd-analysis-effective/v1";
    public const string UnspecifiedIntent = "unspecified";
    public const string UnknownSourceSection = "unknown";

    public static readonly IReadOnlySet<string> Operators =
        new HashSet<string>(StringComparer.Ordinal) { "all_of", "one_of", "at_least_n" };

    public static readonly IReadOnlySet<string> Importances =
        new HashSet<string>(StringComparer.Ordinal) { "must_have", "nice_to_have" };

    public static readonly IReadOnlySet<string> SourceSections =
        new HashSet<string>(StringComparer.Ordinal) { "title", "description", "requirements" };

    public static readonly IReadOnlySet<string> ProviderIntents =
        new HashSet<string>(StringComparer.Ordinal) { "qualification", "experience_duration" };

    public static readonly IReadOnlySet<string> Categories =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "tech_skill",
            "experience",
            "domain_knowledge",
            "language",
            "education",
            "soft_skill"
        };
}
