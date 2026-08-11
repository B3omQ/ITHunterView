using System;
using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingInputSnapshotIntegrityTests
{
    private const string V1GoldenJson = """
        {"schemaVersion":"matching-context/v1","mode":"JdFit","cv":{"sourceKind":"raw_cv","sourceId":null,"fileName":"legacy.pdf","originalText":"legacy candidate","analysisJson":null,"analysisSchemaVersion":null},"jd":{"sourceKind":"raw_jd","sourceId":null,"title":"Legacy Engineer","originalText":"legacy job","analysisJson":null,"analysisSchemaVersion":null},"submittedAtUtc":"2026-08-02T08:00:00Z"}
        """;

    private const string V2GoldenJson = """
        {"schemaVersion":"matching-context/v2","mode":"JdFit","cv":{"sourceKind":"saved_cv","sourceId":"11111111-1111-1111-1111-111111111111","fileName":"legacy-v2.pdf","originalText":"candidate v2","analysisJson":"{\"schema_version\":\"cv-analysis/v2\"}","analysisSchemaVersion":"cv-analysis/v2","fileUrl":"https://example.test/cv.pdf","sourceContentHash":"cv-source-hash","sourceParseStatus":"SUCCESS"},"jd":{"sourceKind":"saved_jd","sourceId":"22222222-2222-2222-2222-222222222222","title":"Legacy V2 Engineer","originalText":"Title: Legacy V2 Engineer\nDescription: Build APIs\nRequirements: C#","analysisJson":"{\"schema_version\":\"jd-analysis-effective/v1\"}","analysisSchemaVersion":"jd-analysis-effective/v1","sourceContentHash":"jd-source-hash","sourceAnalysisHash":"jd-analysis-hash","sourceAnalysisRevision":7,"sourceEffectiveAnalysisRevision":7,"sourceParseStatus":"SUCCESS"},"submittedAtUtc":"2026-08-10T08:00:00Z"}
        """;

    [Fact]
    public void Deserialize_V1GoldenPayload_PreservesHistoricalHash()
    {
        var snapshot = MatchingInputSnapshotIntegrity.Deserialize(V1GoldenJson);

        snapshot.SchemaVersion.Should().Be("matching-context/v1");
        MatchingInputSnapshotIntegrity.ComputeHash(snapshot)
            .Should().Be("0c9e41c40be2808a555b0d0af322626cfdabc3378c8f460902aa60af689f7928");
    }

    [Fact]
    public void Deserialize_V2GoldenPayload_PreservesHistoricalStorageAndHash()
    {
        var snapshot = MatchingInputSnapshotIntegrity.Deserialize(V2GoldenJson);

        snapshot.SchemaVersion.Should().Be("matching-context/v2");
        MatchingInputSnapshotIntegrity.Serialize(snapshot).Should().Be(V2GoldenJson);
        MatchingInputSnapshotIntegrity.ComputeHash(snapshot)
            .Should().Be("6290be1820c1c8ee17b9f716c87c0b312ea03e46915c4f1a26c7b72c403e129b");
    }

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

    [Fact]
    public void V3StorageAndHash_IncludeCanonicalAnalysisInput()
    {
        var snapshot = CreateSnapshot("candidate text", "job text", DateTime.UtcNow) with
        {
            SchemaVersion = "matching-context/v3",
            Jd = new MatchingJdSnapshot(
                "raw_jd",
                null,
                "Engineer",
                "job text",
                null,
                null,
                AnalysisInputJson: "{\"title\":\"Engineer\",\"description\":\"job text\",\"requirements\":\"\"}")
        };

        var json = MatchingInputSnapshotIntegrity.Serialize(snapshot);
        var restored = MatchingInputSnapshotIntegrity.Deserialize(json);
        var tampered = restored with
        {
            Jd = restored.Jd with
            {
                AnalysisInputJson = "{\"title\":\"Engineer\",\"description\":\"tampered\",\"requirements\":\"\"}"
            }
        };

        json.Should().Contain("\"analysisInputJson\"");
        restored.Jd.AnalysisInputJson.Should().Contain("job text");
        MatchingInputSnapshotIntegrity.ComputeHash(tampered)
            .Should().NotBe(MatchingInputSnapshotIntegrity.ComputeHash(restored));
    }

    [Fact]
    public void Deserialize_V2PayloadWithV3OnlyField_IsRejected()
    {
        var mutated = V2GoldenJson.Replace(
            "\"sourceParseStatus\":\"SUCCESS\"},\"submittedAtUtc\"",
            "\"sourceParseStatus\":\"SUCCESS\",\"analysisInputJson\":\"{}\"},\"submittedAtUtc\"",
            StringComparison.Ordinal);

        var action = () => MatchingInputSnapshotIntegrity.Deserialize(mutated);

        action.Should().Throw<InvalidOperationException>().WithMessage("SNAPSHOT_INVALID");
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
