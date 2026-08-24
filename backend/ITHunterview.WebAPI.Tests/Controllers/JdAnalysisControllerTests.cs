using System.Reflection;
using FluentAssertions;
using ITHunterview.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Tests.Controllers;

public sealed class JdAnalysisControllerTests
{
    [Fact]
    public void Analyze_AllowsAnonymousPostmanCallsDuringDevelopment()
    {
        var controllerType = typeof(JdAnalysisController);
        var action = controllerType.GetMethod(nameof(JdAnalysisController.Analyze));

        controllerType.GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/jd-analysis");
        controllerType.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        controllerType.GetCustomAttribute<AuthorizeAttribute>().Should().BeNull();
        action.Should().NotBeNull();
        action!.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("test");
    }
}
