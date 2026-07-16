using ITHunterview.Domain.Entities.Cv;
using Xunit;

namespace ITHunterview.Service.Tests.Domain;

public class CvDocumentHelperTests
{
    [Fact]
    public void GetFieldByPath_ShouldReturnCorrectValue_ForNestedProperty()
    {
        var doc = new CvDocument
        {
            Header = new CvHeader { FullName = "John Doe" },
            Summary = "A great developer"
        };

        var name = CvDocumentHelper.GetFieldByPath(doc, "Header.FullName");
        var summary = CvDocumentHelper.GetFieldByPath(doc, "Summary");

        Assert.Equal("John Doe", name);
        Assert.Equal("A great developer", summary);
    }

    [Fact]
    public void GetFieldByPath_ShouldReturnCorrectValue_ForListElement()
    {
        var doc = new CvDocument
        {
            Header = new CvHeader { FullName = "John" },
            Experience = new List<CvExperience>
            {
                new CvExperience
                {
                    Company = "Tech Corp",
                    Role = "Dev",
                    Bullets = new List<string> { "Did stuff", "Did more stuff" }
                }
            }
        };

        var company = CvDocumentHelper.GetFieldByPath(doc, "Experience[0].Company");
        var bullet = CvDocumentHelper.GetFieldByPath(doc, "Experience[0].Bullets[1]");

        Assert.Equal("Tech Corp", company);
        Assert.Equal("Did more stuff", bullet);
    }

    [Fact]
    public void SetFieldByPath_ShouldUpdateCorrectValue_ForListElement()
    {
        var doc = new CvDocument
        {
            Header = new CvHeader { FullName = "John" },
            Experience = new List<CvExperience>
            {
                new CvExperience
                {
                    Company = "Tech Corp",
                    Role = "Dev",
                    Bullets = new List<string> { "Old bullet" }
                }
            }
        };

        CvDocumentHelper.SetFieldByPath(doc, "Experience[0].Bullets[0]", "New bullet");

        Assert.Equal("New bullet", doc.Experience[0].Bullets[0]);
    }
}
