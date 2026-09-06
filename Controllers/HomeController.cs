using System.Diagnostics;
using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Lib_System.Models;
using System.Security.Claims;

namespace Lib_System.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Index()
    {
        var isMember = User.IsInRole("Member");
        var currentUserId = GetCurrentUserId();
        var dashboard = await _dashboardService.GetDashboardAsync(isMember, currentUserId);

        return View(dashboard);
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
