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

namespace OrchardCore.Tests.Modules.OrchardCore.AdminDashboard;

public class DashboardControllerTests
{
    [Theory]
    [InlineData(null, new string[] { })]
    [InlineData("", new string[] { })]
    [InlineData("segment1", new[] { "segment1" })]
    [InlineData("segment1;segment2", new[] { "segment1", "segment2" })]
    [InlineData("seg1;seg2;seg3;seg4;seg5;seg6;seg7;seg8;seg9;seg10;seg11", new[] { "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10", "seg11" })]
    [InlineData("  trimmed  ", new[] { "trimmed" })]
    public void ParseSegments_ShouldSplitUserAgentCorrectly(string userAgent, string[] expected)
    {
        // Arrange
        var controller = CreateController();
        var method = GetPrivateMethod("ParseSegments");

        // Act
        var result = method.Invoke(controller, new object[] { userAgent }) as string[];

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Length, result.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(new string[] { }, "")]
    [InlineData(new[] { "seg1" }, "")]
    [InlineData(new[] { "seg1", "seg2", "seg3", "seg4", "seg5" }, "")]
    [InlineData(new[] { "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10" }, "")]
    [InlineData(new[] { "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10", "seg11" }, "seg11")]
    [InlineData(new[] { "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10", "  seg11  " }, "seg11")]
    [InlineData(new[] { "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10", "seg11", "seg12" }, "seg11")]
    public void ExtractCriticalSegment_ShouldHandleBoundsCorrectly(string[] segments, string expected)
    {
        // Arrange
        var controller = CreateController();
        var method = GetPrivateMethod("ExtractCriticalSegment");

        // Act
        var result = method.Invoke(controller, new object[] { segments }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractCriticalSegment_ShouldNotThrowIndexOutOfRangeException_WithShortUserAgent()
    {
        // Arrange
        var controller = CreateController();
        var parseMethod = GetPrivateMethod("ParseSegments");
        var extractMethod = GetPrivateMethod("ExtractCriticalSegment");
        
        // Simulate a short User-Agent string with fewer than 11 segments
        var shortUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        
        // Act
        var segments = parseMethod.Invoke(controller, new object[] { shortUserAgent }) as string[];
        var exception = Record.Exception(() => extractMethod.Invoke(controller, new object[] { segments }));
        
        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ExtractCriticalSegment_ShouldNotThrowIndexOutOfRangeException_WithNullUserAgent()
    {
        // Arrange
        var controller = CreateController();
        var parseMethod = GetPrivateMethod("ParseSegments");
        var extractMethod = GetPrivateMethod("ExtractCriticalSegment");
        
        // Act
        var segments = parseMethod.Invoke(controller, new object[] { null }) as string[];
        var exception = Record.Exception(() => extractMethod.Invoke(controller, new object[] { segments }));
        
        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ExtractCriticalSegment_ShouldNotThrowIndexOutOfRangeException_WithEmptyUserAgent()
    {
        // Arrange
        var controller = CreateController();
        var parseMethod = GetPrivateMethod("ParseSegments");
        var extractMethod = GetPrivateMethod("ExtractCriticalSegment");
        
        // Act
        var segments = parseMethod.Invoke(controller, new object[] { "" }) as string[];
        var exception = Record.Exception(() => extractMethod.Invoke(controller, new object[] { segments }));
        
        // Assert
        Assert.Null(exception);
    }

    private static DashboardController CreateController()
    {
        var authorizationService = new Mock<IAuthorizationService>();
        var adminDashboardService = new Mock<IAdminDashboardService>();
        var contentManager = new Mock<IContentManager>();
        var contentItemDisplayManager = new Mock<IContentItemDisplayManager>();
        var contentDefinitionManager = new Mock<IContentDefinitionManager>();
        var updateModelAccessor = new Mock<IUpdateModelAccessor>();
        var session = new Mock<YesSql.ISession>();
        var logger = new Mock<ILogger<DashboardController>>();

        var controller = new DashboardController(
            authorizationService.Object,
            adminDashboardService.Object,
            contentManager.Object,
            contentItemDisplayManager.Object,
            contentDefinitionManager.Object,
            updateModelAccessor.Object,
            session.Object,
            logger.Object);

        // Set up HttpContext to avoid null reference issues
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        return controller;
    }

    private static MethodInfo GetPrivateMethod(string methodName)
    {
        var method = typeof(DashboardController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method {methodName} not found");
        }
        return method;
    }
}
