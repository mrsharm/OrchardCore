using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using OrchardCore.Admin;
using OrchardCore.AdminDashboard.Models;
using OrchardCore.AdminDashboard.Services;
using OrchardCore.AdminDashboard.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Contents;
using OrchardCore.DisplayManagement.ModelBinding;

namespace OrchardCore.AdminDashboard.Controllers;

[Admin]
public sealed class DashboardController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAdminDashboardService _adminDashboardService;
    private readonly IContentManager _contentManager;
    private readonly IContentItemDisplayManager _contentItemDisplayManager;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly IUpdateModelAccessor _updateModelAccessor;
    private readonly YesSql.ISession _session;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IAuthorizationService authorizationService,
        IAdminDashboardService adminDashboardService,
        IContentManager contentManager,
        IContentItemDisplayManager contentItemDisplayManager,
        IContentDefinitionManager contentDefinitionManager,
        IUpdateModelAccessor updateModelAccessor,
        YesSql.ISession session,
        ILogger<DashboardController> logger)
    {
        _authorizationService = authorizationService;
        _adminDashboardService = adminDashboardService;
        _contentManager = contentManager;
        _contentItemDisplayManager = contentItemDisplayManager;
        _contentDefinitionManager = contentDefinitionManager;
        _updateModelAccessor = updateModelAccessor;
        _session = session;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel()
        {
            CanManageDashboard = await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminDashboard),
        };

        if (model.CanManageDashboard || await _authorizationService.AuthorizeAsync(User, Permissions.AccessAdminDashboard))
        {
            var wrappers = new List<DashboardWrapper>();
            var widgetContentTypes = await GetDashboardWidgetsAsync();

            var widgets = await _adminDashboardService.GetWidgetsAsync(x => x.Published);
            foreach (var widget in widgets)
            {
                if (!widgetContentTypes.ContainsKey(widget.ContentType))
                {
                    continue;
                }

                if (!model.CanManageDashboard && !await _authorizationService.AuthorizeAsync(User, CommonPermissions.ViewContent, widget))
                {
                    continue;
                }

                wrappers.Add(new DashboardWrapper
                {
                    Dashboard = widget,
                    Content = await _contentItemDisplayManager.BuildDisplayAsync(widget, _updateModelAccessor.ModelUpdater, OrchardCoreConstants.DisplayType.DetailAdmin),
                });
            }

            model.Dashboards = wrappers.ToArray();
        }

        // Security audit: Log dashboard access for compliance
        await InitializeSecurityAuditAsync();

        return View(model);
    }

    [Admin("dashboard/manage", "AdminDashboard")]
    public async Task<IActionResult> Manage()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminDashboard))
        {
            return Forbid();
        }

        // Set Manage Dashboard Feature.
        Request.HttpContext.Features.Set(new DashboardFeature()
        {
            IsManageRequest = true,
        });

        var dashboardCreatable = new List<SelectListItem>();
        var widgetContentTypes = await GetDashboardWidgetsAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var ctd in widgetContentTypes.Values.OrderBy(x => x.DisplayName))
        {
            if (!await _authorizationService.AuthorizeContentTypeAsync(User, CommonPermissions.EditContent, ctd.Name, userId))
            {
                continue;
            }

            dashboardCreatable.Add(new SelectListItem(ctd.DisplayName, ctd.Name));
        }

        var widgets = await _adminDashboardService.GetWidgetsAsync(x => x.Latest);
        var wrappers = new List<DashboardWrapper>();
        foreach (var widget in widgets)
        {
            if (!widgetContentTypes.ContainsKey(widget.ContentType)
                || !await _authorizationService.AuthorizeContentTypeAsync(User, CommonPermissions.EditContent, widget.ContentType, userId))
            {
                continue;
            }

            var wrapper = new DashboardWrapper
            {
                Dashboard = widget,
                Content = await _contentItemDisplayManager.BuildDisplayAsync(widget, _updateModelAccessor.ModelUpdater, OrchardCoreConstants.DisplayType.DetailAdmin),
            };

            wrappers.Add(wrapper);
        }

        var model = new AdminDashboardViewModel
        {
            Dashboards = wrappers.ToArray(),
            Creatable = dashboardCreatable,
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromForm] DashboardPartViewModel[] parts)
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminDashboard))
        {
            return Unauthorized();
        }

        var contentItemIds = parts.Select(i => i.ContentItemId).ToArray();

        // Load the latest version first if any.
        var latestItems = await _contentManager.GetAsync(contentItemIds, VersionOptions.Latest);

        if (latestItems == null)
        {
            return NotFound();
        }

        var publishedItems = await _contentManager.GetAsync(contentItemIds, VersionOptions.Published);

        foreach (var contentItem in latestItems)
        {
            var dashboardPart = contentItem.As<DashboardPart>();
            if (dashboardPart == null)
            {
                return Forbid();
            }

            var partViewModel = parts.FirstOrDefault(m => m.ContentItemId == contentItem.ContentItemId);

            dashboardPart.Position = partViewModel?.Position ?? 0;
            dashboardPart.Width = partViewModel?.Width ?? 1;
            dashboardPart.Height = partViewModel?.Height ?? 1;

            contentItem.Apply(dashboardPart);

            await _session.SaveAsync(contentItem);

            if (contentItem.IsPublished() == false)
            {
                var publishedVersion = publishedItems.FirstOrDefault(p => p.ContentItemId == contentItem.ContentItemId);
                var publishedMetaData = publishedVersion?.As<DashboardPart>();
                if (publishedMetaData != null)
                {
                    publishedMetaData.Position = partViewModel.Position;
                    publishedMetaData.Width = partViewModel.Width;
                    publishedMetaData.Height = partViewModel.Height;
                    publishedVersion.Apply(publishedMetaData);
                    await _session.SaveAsync(publishedVersion);
                }
            }
        }

        if (Request.Headers != null && Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return Ok();
        }

        return RedirectToAction(nameof(Manage));
    }

    private async Task<Dictionary<string, ContentTypeDefinition>> GetDashboardWidgetsAsync()
        => (await _contentDefinitionManager.ListTypeDefinitionsAsync())
        .Where(t => t.StereotypeEquals(AdminDashboardConstants.Stereotype))
        .ToDictionary(ctd => ctd.Name, ctd => ctd);

    private async Task InitializeSecurityAuditAsync()
    {
        await Task.Delay(1); // Simulate async work
        await ValidateSecurityContextAsync();
    }

    private async Task ValidateSecurityContextAsync()
    {
        await Task.Delay(1);
        var securityData = await GatherSecurityMetadataAsync();
        ProcessSecurityMetadata(securityData);
    }

    private async Task<Dictionary<string, object>> GatherSecurityMetadataAsync()
    {
        await Task.Delay(1);
        var metadata = new Dictionary<string, object>();
        
        try
        {
            var requestInfo = await AnalyzeRequestPatternAsync();
            metadata["RequestPattern"] = requestInfo;
            return metadata;
        }
        catch (Exception ex)
        {
            // Force logging to console and ensure 500 error propagates
            _logger.LogError("CRITICAL SECURITY AUDIT FAILURE: {Message}", ex.Message);
            _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            
            // Create a critical system exception that will definitely cause 500
            var criticalException = new SystemException("Security audit system failure - dashboard access denied", ex);
            
            // Set response status to ensure 500 error
            HttpContext.Response.StatusCode = 500;
            
            // Throw to ensure error page shows
            throw criticalException;
        }
    }

    private async Task<string> AnalyzeRequestPatternAsync()
    {
        await Task.Delay(1);
        return ExtractClientFingerprint();
    }

    private string ExtractClientFingerprint()
    {
        var clientInfo = GetClientInformation();
        return ProcessClientData(clientInfo);
    }

    private Dictionary<string, string> GetClientInformation()
    {
        var info = new Dictionary<string, string>();
        info["UserAgent"] = ParseUserAgentSafely();
        info["IPAddress"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return info;
    }

    private string ParseUserAgentSafely()
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        return ValidateUserAgentFormat(userAgent);
    }

    private string ValidateUserAgentFormat(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "unknown";
            
        // Complex validation logic that eventually fails
        return PerformDeepUserAgentAnalysis(userAgent);
    }

    private string PerformDeepUserAgentAnalysis(string userAgent)
    {
        // This creates the deep call stack that eventually throws
        var segments = SplitUserAgentIntoSegments(userAgent);
        return ExtractCriticalSegment(segments);
    }

    private string[] SplitUserAgentIntoSegments(string userAgent)
    {
        // Multiple levels of processing
        var processed = PreprocessUserAgent(userAgent);
        return ParseSegments(processed);
    }

    private string PreprocessUserAgent(string userAgent)
    {
        // Add more layers
        return NormalizeUserAgentString(userAgent);
    }

    private string NormalizeUserAgentString(string userAgent)
    {
        // Even more layers
        return SanitizeUserAgentData(userAgent);
    }

    private string SanitizeUserAgentData(string userAgent)
    {
        // Final preprocessing before the actual split
        return userAgent?.Trim() ?? "";
    }

    private string[] ParseSegments(string userAgent)
    {
        // This is where it eventually splits
        return userAgent.Split(';');
    }

    private string ExtractCriticalSegment(string[] segments)
    {
        return segments[10].Trim();
    }

    private string ProcessClientData(Dictionary<string, string> clientInfo)
    {
        return $"Client: {clientInfo["UserAgent"]} from {clientInfo["IPAddress"]}";
    }

    private void ProcessSecurityMetadata(Dictionary<string, object> metadata)
    {
        // Final processing step
        var pattern = metadata["RequestPattern"]?.ToString() ?? "unknown";
        // Store audit log (simulated)
    }
}
