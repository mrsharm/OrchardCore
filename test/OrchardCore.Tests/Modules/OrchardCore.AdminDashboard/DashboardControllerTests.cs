using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using OrchardCore.AdminDashboard.Controllers;
using OrchardCore.AdminDashboard.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.ModelBinding;
using YesSql;

namespace OrchardCore.Tests.Modules.OrchardCore.AdminDashboard;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_WithEmptyUserAgent_ShouldNotThrowException()
    {
        // Arrange
        var controller = CreateDashboardController();
        SetUserAgent(controller, "");

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithShortUserAgent_ShouldNotThrowException()
    {
        // Arrange
        var controller = CreateDashboardController();
        SetUserAgent(controller, "Mozilla;5.0;Windows");

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithNullUserAgent_ShouldNotThrowException()
    {
        // Arrange
        var controller = CreateDashboardController();
        // Don't set User-Agent header at all

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithLongUserAgent_ShouldNotThrowException()
    {
        // Arrange
        var controller = CreateDashboardController();
        var longUserAgent = string.Join(";", Enumerable.Range(0, 15).Select(i => $"segment{i}"));
        SetUserAgent(controller, longUserAgent);

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithTypicalUserAgent_ShouldNotThrowException()
    {
        // Arrange
        var controller = CreateDashboardController();
        SetUserAgent(controller, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        // Act
        var result = await controller.Index();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
    }

    private static DashboardController CreateDashboardController()
    {
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        authorizationServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var adminDashboardServiceMock = new Mock<IAdminDashboardService>();
        adminDashboardServiceMock
            .Setup(a => a.GetWidgetsAsync(It.IsAny<Func<ContentItem, bool>>()))
            .ReturnsAsync([]);

        var contentManagerMock = new Mock<IContentManager>();
        var contentItemDisplayManagerMock = new Mock<IContentItemDisplayManager>();
        var contentDefinitionManagerMock = new Mock<IContentDefinitionManager>();
        contentDefinitionManagerMock
            .Setup(c => c.ListTypeDefinitionsAsync())
            .ReturnsAsync([]);

        var updateModelAccessorMock = new Mock<IUpdateModelAccessor>();
        var sessionMock = new Mock<ISession>();
        var loggerMock = new Mock<ILogger<DashboardController>>();

        var controller = new DashboardController(
            authorizationServiceMock.Object,
            adminDashboardServiceMock.Object,
            contentManagerMock.Object,
            contentItemDisplayManagerMock.Object,
            contentDefinitionManagerMock.Object,
            updateModelAccessorMock.Object,
            sessionMock.Object,
            loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static void SetUserAgent(DashboardController controller, string userAgent)
    {
        controller.ControllerContext.HttpContext.Request.Headers["User-Agent"] = new StringValues(userAgent);
    }
}
