using System;
using System.Collections.Generic;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Manual retry is reserved for failures that can plausibly change without
/// changing the CV, JD, or prompt contract. Deterministic validation failures
/// must not create another charged matching job.
/// </summary>
public static class MatchingRetryPolicy
{
    private static readonly IReadOnlySet<string> ManualRetryableCodes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "AI_PROVIDER_TIMEOUT",
            "AI_PROVIDER_REQUEST_FAILED",
            "AI_PROVIDER_HTTP_ERROR",
            "AI_PROVIDER_INVALID_JSON",
            "LEASE_EXPIRED"
        };

    public static bool IsManualRetryAllowed(string? errorCode)
        => !string.IsNullOrWhiteSpace(errorCode) && ManualRetryableCodes.Contains(errorCode);
}
