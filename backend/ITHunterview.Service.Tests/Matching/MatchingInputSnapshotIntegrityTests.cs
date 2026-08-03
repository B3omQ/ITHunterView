using System;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingInputSnapshotIntegrityTests
{
    [Fact]
    public void SerializeAndDeserialize_PreservesCanonicalSnapshotHash()
    {
        var snapshot = CreateSnapshot("candidate text", "job text", DateTime.UtcNow);

        var json = MatchingInputSnapshotIntegrity.Serialize(snapshot);
        var restored = MatchingInputSnapshotIntegrity.Deserialize(json);

        MatchingInputSnapshotIntegrity.IsValid(restored, MatchingInputSnapshotIntegrity.ComputeHash(snapshot))
            .Should().BeTrue();
    }

    [Fact]
    public void Hash_ExcludesSubmissionTimestampButDetectsInputChanges()
    {
        var first = CreateSnapshot("candidate text", "job text", new DateTime(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc));
        var sameInputsLater = first with { SubmittedAtUtc = first.SubmittedAtUtc.AddHours(1) };
        var changedCv = first with { Cv = first.Cv with { OriginalText = "tampered candidate text" } };
        var changedJd = first with { Jd = first.Jd with { OriginalText = "tampered job text" } };

        var hash = MatchingInputSnapshotIntegrity.ComputeHash(first);

        MatchingInputSnapshotIntegrity.ComputeHash(sameInputsLater).Should().Be(hash);
        MatchingInputSnapshotIntegrity.ComputeHash(changedCv).Should().NotBe(hash);
        MatchingInputSnapshotIntegrity.ComputeHash(changedJd).Should().NotBe(hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-sha256")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public void IsValid_RejectsMalformedOrMismatchedHashes(string? expectedHash)
    {
        var snapshot = CreateSnapshot("candidate text", "job text", DateTime.UtcNow);

        MatchingInputSnapshotIntegrity.IsValid(snapshot, expectedHash).Should().BeFalse();
    }

    private static MatchingInputSnapshotV1 CreateSnapshot(
        string cvText,
        string jdText,
        DateTime submittedAtUtc)
        => new(
            MatchingInputSnapshotBuilder.SchemaVersion,
            MatchingMode.JdFit,
            new MatchingCvSnapshot("raw_cv", null, "cv.pdf", cvText, null, null),
            new MatchingJdSnapshot("raw_jd", null, "Engineer", jdText, null, null),
            submittedAtUtc);
}
