namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record RetainedCvSnapshot(
    string StorageKey,
    string FileName,
    string ContentHash,
    DateTime CreatedAt);
