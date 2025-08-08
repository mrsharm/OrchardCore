using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrchardCore.Admin.Controllers;

[Authorize]
public sealed class AdminController : Controller
{
    public IActionResult Index()
    {
        // Redirect to Dashboard to trigger security audit
        return RedirectToAction("Index", "Dashboard", new { area = "OrchardCore.AdminDashboard" });
    }
}
