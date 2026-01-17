using System.Linq.Expressions;
using System.Security.Claims;
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
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.ModelBinding;

namespace OrchardCore.Tests.Modules.OrchardCore.AdminDashboard;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_WithEmptyUserAgent_DoesNotThrowException()
    {
        // Arrange
        var controller = CreateController();
        SetUserAgent(controller, "");

        // Act & Assert
        var result = await controller.Index();
        
        // Should not throw IndexOutOfRangeException
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Index_WithNullUserAgent_DoesNotThrowException()
    {
        // Arrange
        var controller = CreateController();
        SetUserAgent(controller, null);

        // Act & Assert
        var result = await controller.Index();
        
        // Should not throw IndexOutOfRangeException
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Index_WithShortUserAgent_DoesNotThrowException()
    {
        // Arrange
        var controller = CreateController();
        // User-Agent with only 3 segments (less than required 11)
        SetUserAgent(controller, "Mozilla;5.0;Windows");

        // Act & Assert
        var result = await controller.Index();
        
        // Should not throw IndexOutOfRangeException
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Index_WithTypicalUserAgent_DoesNotThrowException()
    {
        // Arrange
        var controller = CreateController();
        // Typical browser User-Agent
        SetUserAgent(controller, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        // Act & Assert
        var result = await controller.Index();
        
        // Should not throw IndexOutOfRangeException
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Index_WithLongUserAgent_DoesNotThrowException()
    {
        // Arrange
        var controller = CreateController();
        // User-Agent with 15 segments (more than required 11)
        SetUserAgent(controller, "seg0;seg1;seg2;seg3;seg4;seg5;seg6;seg7;seg8;seg9;seg10;seg11;seg12;seg13;seg14");

        // Act & Assert
        var result = await controller.Index();
        
        // Should not throw IndexOutOfRangeException
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("a;b")]
    [InlineData("a;b;c;d;e;f;g;h;i;j")]
    [InlineData("Mozilla/5.0")]
    public async Task Index_WithVariousShortUserAgents_DoesNotThrowException(string userAgent)
    {
        // Arrange
        var controller = CreateController();
        SetUserAgent(controller, userAgent);

        // Act & Assert
        var result = await controller.Index();
        
        // Should not throw IndexOutOfRangeException
        Assert.NotNull(result);
    }

    private static DashboardController CreateController()
    {
        var mockAuthorizationService = new Mock<IAuthorizationService>();
        mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var mockAdminDashboardService = new Mock<IAdminDashboardService>();
        mockAdminDashboardService
            .Setup(x => x.GetWidgetsAsync(It.IsAny<Expression<Func<ContentItemIndex, bool>>>()))
            .ReturnsAsync(Array.Empty<ContentItem>());

        var mockContentManager = new Mock<IContentManager>();
        var mockContentItemDisplayManager = new Mock<IContentItemDisplayManager>();
        var mockContentDefinitionManager = new Mock<IContentDefinitionManager>();
        mockContentDefinitionManager
            .Setup(x => x.ListTypeDefinitionsAsync())
            .ReturnsAsync(new List<ContentTypeDefinition>());

        var mockUpdateModelAccessor = new Mock<IUpdateModelAccessor>();
        var mockSession = new Mock<YesSql.ISession>();
        var mockLogger = new Mock<ILogger<DashboardController>>();

        var controller = new DashboardController(
            mockAuthorizationService.Object,
            mockAdminDashboardService.Object,
            mockContentManager.Object,
            mockContentItemDisplayManager.Object,
            mockContentDefinitionManager.Object,
            mockUpdateModelAccessor.Object,
            mockSession.Object,
            mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static void SetUserAgent(DashboardController controller, string userAgent)
    {
        if (userAgent != null)
        {
            controller.Request.Headers["User-Agent"] = userAgent;
        }
    }
}
