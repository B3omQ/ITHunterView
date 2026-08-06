using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public class MatchingRequestValidatorTests
{
    private readonly MatchingRequestValidator _validator = new();

    [Theory]
    [MemberData(nameof(InvalidSourceRequests))]
    public void Validate_InvalidSourceCombination_ReturnsExpectedCode(
        MatchingRequestDto request,
        string expectedCode)
    {
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.FailureCode.Should().Be(expectedCode);
    }

    public static IEnumerable<object[]> InvalidSourceRequests()
    {
        var noCv = ValidRequest();
        noCv.CvText = null;
        yield return new object[]
        {
            noCv,
            "CV_SOURCE_REQUIRED"
        };

        var multipleCv = ValidRequest();
        multipleCv.CvId = Guid.NewGuid();
        yield return new object[]
        {
            multipleCv,
            "MULTIPLE_CV_SOURCES"
        };

        var cvUrl = ValidRequest();
        cvUrl.CvUrl = "https://untrusted.example/cv.pdf";
        yield return new object[]
        {
            cvUrl,
            "CV_URL_SOURCE_NOT_SUPPORTED"
        };

        var whitespaceCv = ValidRequest();
        whitespaceCv.CvText = "   ";
        yield return new object[]
        {
            whitespaceCv,
            "CV_SOURCE_REQUIRED"
        };

        var noJd = ValidRequest();
        noJd.RawJdText = null;
        yield return new object[]
        {
            noJd,
            "JD_SOURCE_REQUIRED"
        };

        var multipleJd = ValidRequest();
        multipleJd.JobId = Guid.NewGuid();
        yield return new object[]
        {
            multipleJd,
            "MULTIPLE_JD_SOURCES"
        };
    }

    [Fact]
    public void Validate_ValidRawSources_TrimsAndReturnsSelection()
    {
        var request = ValidRequest();
        request.CvText = $"  {new string('c', 100)}  ";
        request.RawJdText = $"  {new string('j', 100)}  ";
        request.CvFileName = "  cv.pdf  ";
        request.JdTitle = "  Backend Engineer  ";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Selection!.CvText.Should().Be(new string('c', 100));
        result.Selection.RawJdText.Should().Be(new string('j', 100));
        result.Selection.CvFileName.Should().Be("cv.pdf");
        result.Selection.JdTitle.Should().Be("Backend Engineer");
    }

    [Theory]
    [InlineData(99, "CV_TEXT_TOO_SHORT")]
    [InlineData(100001, "CV_TEXT_TOO_LONG")]
    public void Validate_RawCvOutsideBounds_ReturnsExpectedCode(int length, string expectedCode)
    {
        var request = ValidRequest();
        request.CvText = new string('c', length);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.FailureCode.Should().Be(expectedCode);
    }

    [Fact]
    public void Validate_UnsupportedMode_WinsBeforeSourceValidation()
    {
        var request = ValidRequest();
        request.CvText = null;
        request.Mode = MatchingMode.Both;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.FailureCode.Should().Be("MATCHING_MODE_NOT_SUPPORTED");
    }

    private static MatchingRequestDto ValidRequest() => new()
    {
        CvText = new string('c', 100),
        RawJdText = new string('j', 100),
        Mode = MatchingMode.JdFit
    };
}
