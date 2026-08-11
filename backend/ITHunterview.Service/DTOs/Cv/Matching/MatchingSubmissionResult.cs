using System;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record MatchingSubmissionResult(Guid JobId, bool IsExisting);
