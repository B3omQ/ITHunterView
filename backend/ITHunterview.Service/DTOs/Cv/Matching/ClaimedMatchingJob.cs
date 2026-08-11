using System;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record ClaimedMatchingJob(Guid JobId, Guid LeaseToken);
