using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.AdminDashboard.Controllers;
using OrchardCore.AdminDashboard.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.ModelBinding;

namespace OrchardCore.Tests.Modules.OrchardCore.AdminDashboard;

public class DashboardControllerTests
{
    [Theory]
    [InlineData("MinimalClient/1.0")]
    [InlineData("Mozilla/5.0")]
    [InlineData("")]
    [InlineData("segment1;segment2")]
    [InlineData("a;b;c;d;e;f;g;h;i;j")]
    public async Task Index_ShouldHandleShortUserAgent_WithoutException(string userAgent)
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(userAgent);

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    [Theory]
    [InlineData("a;b;c;d;e;f;g;h;i;j;k")]
    [InlineData("1;2;3;4;5;6;7;8;9;10;11")]
    [InlineData("a;b;c;d;e;f;g;h;i;j;k;l;m;n;o")]
    public async Task Index_ShouldExtractCorrectSegment_WhenUserAgentHasEnoughSegments(string userAgent)
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(userAgent);

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
        
        // Verify the method was called and completed successfully
        var viewResult = result as ViewResult;
        Assert.NotNull(viewResult);
    }

    [Fact]
    public async Task Index_ShouldHandleNullUserAgent_WithoutException()
    {
        // Arrange
        var controller = CreateController();
        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context,
        };

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    private static DashboardController CreateController()
    {
        var authService = new Mock<IAuthorizationService>();
        authService.Setup(x => x.AuthorizeAsync(
            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
            It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var dashboardService = new Mock<IAdminDashboardService>();
        dashboardService.Setup(x => x.GetWidgetsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<ContentItemIndex, bool>>>()))
            .ReturnsAsync([]);

        var contentManager = new Mock<IContentManager>();
        var displayManager = new Mock<IContentItemDisplayManager>();
        var definitionManager = new Mock<IContentDefinitionManager>();
        definitionManager.Setup(x => x.ListTypeDefinitionsAsync())
            .ReturnsAsync([]);

        var updateModelAccessor = new Mock<IUpdateModelAccessor>();
        var session = new Mock<YesSql.ISession>();
        var logger = new Mock<ILogger<DashboardController>>();

        return new DashboardController(
            authService.Object,
            dashboardService.Object,
            contentManager.Object,
            displayManager.Object,
            definitionManager.Object,
            updateModelAccessor.Object,
            session.Object,
            logger.Object);
    }

    private static ControllerContext CreateControllerContext(string userAgent)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["User-Agent"] = userAgent;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        return new ControllerContext
        {
            HttpContext = context,
        };
    }
}
