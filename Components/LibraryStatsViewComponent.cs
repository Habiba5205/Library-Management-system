using Lib_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Components;

public class LibraryStatsViewModel
{
    public int BookCount { get; set; }
    public int ActiveMemberCount { get; set; }
}

/// <summary>
/// Small, read-only stats for the login/register brand panel (books in
/// catalog, active members). Queries the database directly rather than
/// going through a repository/service - this is view-support data for a
/// marketing-style panel, not a business operation, so the extra layering
/// wouldn't earn its keep here.
/// </summary>
public class LibraryStatsViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var stats = new LibraryStatsViewModel
        {
            BookCount = await db.Books.CountAsync(),
            ActiveMemberCount = await db.Users
                .CountAsync(u => u.Status == "Active" && u.Role!.RoleName == "Member")
        };
        return View(stats);
    }
}
