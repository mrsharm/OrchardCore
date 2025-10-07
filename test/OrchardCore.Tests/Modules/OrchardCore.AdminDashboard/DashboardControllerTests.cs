using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.AdminDashboard.Controllers;
using OrchardCore.AdminDashboard.Models;
using OrchardCore.AdminDashboard.Services;
using OrchardCore.AdminDashboard.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.ModelBinding;
using YesSqlSession = YesSql.ISession;

namespace OrchardCore.Tests.Modules.OrchardCore.AdminDashboard;

public class DashboardControllerTests
{
    [Fact]
    public async Task Update_Should_Validate_Position_IsNotNegative()
    {
        // Arrange
        var controller = CreateDashboardController();
        var contentItemId = "test-item-id";
        
        var parts = new[]
        {
            new DashboardPartViewModel
            {
                ContentItemId = contentItemId,
                Position = -5, // Negative position should be clamped to 0
                Width = 2,
                Height = 2
            }
        };

        var contentItem = new ContentItem
        {
            ContentItemId = contentItemId,
            Published = true
        };
        
        var dashboardPart = new DashboardPart();
        contentItem.Weld(dashboardPart);

        SetupMocksForUpdate(controller, new[] { contentItem }, new[] { contentItem });

        // Act
        await controller.Update(parts);

        // Assert
        Assert.Equal(0, dashboardPart.Position); // Should be clamped to 0
        Assert.Equal(2, dashboardPart.Width);
        Assert.Equal(2, dashboardPart.Height);
    }

    [Fact]
    public async Task Update_Should_Validate_Width_IsPositive()
    {
        // Arrange
        var controller = CreateDashboardController();
        var contentItemId = "test-item-id";
        
        var parts = new[]
        {
            new DashboardPartViewModel
            {
                ContentItemId = contentItemId,
                Position = 0,
                Width = 0, // Zero width should be clamped to 1
                Height = 2
            }
        };

        var contentItem = new ContentItem
        {
            ContentItemId = contentItemId,
            Published = true
        };
        
        var dashboardPart = new DashboardPart();
        contentItem.Weld(dashboardPart);

        SetupMocksForUpdate(controller, new[] { contentItem }, new[] { contentItem });

        // Act
        await controller.Update(parts);

        // Assert
        Assert.Equal(0, dashboardPart.Position);
        Assert.Equal(1, dashboardPart.Width); // Should be clamped to 1
        Assert.Equal(2, dashboardPart.Height);
    }

    [Fact]
    public async Task Update_Should_Validate_Height_IsPositive()
    {
        // Arrange
        var controller = CreateDashboardController();
        var contentItemId = "test-item-id";
        
        var parts = new[]
        {
            new DashboardPartViewModel
            {
                ContentItemId = contentItemId,
                Position = 0,
                Width = 2,
                Height = -1 // Negative height should be clamped to 1
            }
        };

        var contentItem = new ContentItem
        {
            ContentItemId = contentItemId,
            Published = true
        };
        
        var dashboardPart = new DashboardPart();
        contentItem.Weld(dashboardPart);

        SetupMocksForUpdate(controller, new[] { contentItem }, new[] { contentItem });

        // Act
        await controller.Update(parts);

        // Assert
        Assert.Equal(0, dashboardPart.Position);
        Assert.Equal(2, dashboardPart.Width);
        Assert.Equal(1, dashboardPart.Height); // Should be clamped to 1
    }

    [Fact]
    public async Task Update_Should_Accept_Valid_Values()
    {
        // Arrange
        var controller = CreateDashboardController();
        var contentItemId = "test-item-id";
        
        var parts = new[]
        {
            new DashboardPartViewModel
            {
                ContentItemId = contentItemId,
                Position = 5,
                Width = 3,
                Height = 2
            }
        };

        var contentItem = new ContentItem
        {
            ContentItemId = contentItemId,
            Published = true
        };
        
        var dashboardPart = new DashboardPart();
        contentItem.Weld(dashboardPart);

        SetupMocksForUpdate(controller, new[] { contentItem }, new[] { contentItem });

        // Act
        await controller.Update(parts);

        // Assert
        Assert.Equal(5, dashboardPart.Position);
        Assert.Equal(3, dashboardPart.Width);
        Assert.Equal(2, dashboardPart.Height);
    }

    private DashboardController CreateDashboardController()
    {
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var adminDashboardService = new Mock<IAdminDashboardService>();
        var contentManager = new Mock<IContentManager>();
        var contentItemDisplayManager = new Mock<IContentItemDisplayManager>();
        var contentDefinitionManager = new Mock<IContentDefinitionManager>();
        var updateModelAccessor = new Mock<IUpdateModelAccessor>();
        var session = new Mock<YesSqlSession>();
        var logger = new Mock<ILogger<DashboardController>>();

        var controller = new DashboardController(
            authorizationService.Object,
            adminDashboardService.Object,
            contentManager.Object,
            contentItemDisplayManager.Object,
            contentDefinitionManager.Object,
            updateModelAccessor.Object,
            session.Object,
            logger.Object
        );

        // Setup HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private void SetupMocksForUpdate(DashboardController controller, ContentItem[] latestItems, ContentItem[] publishedItems)
    {
        var contentManager = new Mock<IContentManager>();
        contentManager
            .Setup(x => x.GetAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<VersionOptions>()))
            .ReturnsAsync((IEnumerable<string> ids, VersionOptions options) =>
            {
                if (options == VersionOptions.Latest)
                {
                    return latestItems;
                }
                return publishedItems;
            });

        // Use reflection to set the private field since we can't easily mock the constructor-injected dependency
        var contentManagerField = typeof(DashboardController).GetField("_contentManager", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentManagerField?.SetValue(controller, contentManager.Object);

        var session = new Mock<YesSqlSession>();
        var sessionField = typeof(DashboardController).GetField("_session", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sessionField?.SetValue(controller, session.Object);
    }
}
