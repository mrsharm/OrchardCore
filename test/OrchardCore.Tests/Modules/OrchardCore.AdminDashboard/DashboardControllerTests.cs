using System.Reflection;
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
using OrchardCore.DisplayManagement.ModelBinding;

namespace OrchardCore.Modules.AdminDashboard.Tests;

public class DashboardControllerTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("Mozilla", "Mozilla")]
    [InlineData("a;b", "b")]
    [InlineData("a;b;c;d;e", "e")]
    [InlineData("a;b;c;d;e;f;g;h;i;j", "j")]
    [InlineData("a;b;c;d;e;f;g;h;i;j;k", "k")]
    [InlineData("a;b;c;d;e;f;g;h;i;j;k;l;m;n;o", "k")]
    [InlineData("  a  ;  b  ;  c  ", "c")]
    public void ExtractCriticalSegment_HandlesVariousSegmentCounts(string userAgent, string expected)
    {
        // Arrange
        var controller = CreateController();
        var parseSegmentsMethod = typeof(DashboardController).GetMethod("ParseSegments", BindingFlags.NonPublic | BindingFlags.Instance);
        var extractCriticalSegmentMethod = typeof(DashboardController).GetMethod("ExtractCriticalSegment", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var segments = (string[])parseSegmentsMethod.Invoke(controller, [userAgent]);
        var result = (string)extractCriticalSegmentMethod.Invoke(controller, new object[] { segments });

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractCriticalSegment_EmptyArray_ReturnsEmptyString()
    {
        // Arrange
        var controller = CreateController();
        var extractCriticalSegmentMethod = typeof(DashboardController).GetMethod("ExtractCriticalSegment", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)extractCriticalSegmentMethod.Invoke(controller, new object[] { Array.Empty<string>() });

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractCriticalSegment_OnlyWhitespace_ReturnsEmptyString()
    {
        // Arrange
        var controller = CreateController();
        var extractCriticalSegmentMethod = typeof(DashboardController).GetMethod("ExtractCriticalSegment", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)extractCriticalSegmentMethod.Invoke(controller, new object[] { new[] { "  ", "   ", "\t" } });

        // Assert
        Assert.Equal(string.Empty, result);
    }

    private static DashboardController CreateController()
    {
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        var adminDashboardServiceMock = new Mock<IAdminDashboardService>();
        var contentManagerMock = new Mock<IContentManager>();
        var contentItemDisplayManagerMock = new Mock<IContentItemDisplayManager>();
        var contentDefinitionManagerMock = new Mock<IContentDefinitionManager>();
        var updateModelAccessorMock = new Mock<IUpdateModelAccessor>();
        var sessionMock = new Mock<YesSql.ISession>();
        var loggerMock = new Mock<ILogger<DashboardController>>();

        var controller = new DashboardController(
            authorizationServiceMock.Object,
            adminDashboardServiceMock.Object,
            contentManagerMock.Object,
            contentItemDisplayManagerMock.Object,
            contentDefinitionManagerMock.Object,
            updateModelAccessorMock.Object,
            sessionMock.Object,
            loggerMock.Object
        );

        // Set up HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        return controller;
    }
}
