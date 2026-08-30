using System.Diagnostics;
using Lib_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lib_System.Models;
using Lib_System.ViewModels;
using System.Security.Claims;

namespace Lib_System.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Index()
    {
        var isMember = User.IsInRole("Member");
        var currentUserId = GetCurrentUserId();
        var borrowingsQuery = _context.Borrowings.AsQueryable();
        var paymentsQuery = _context.Payments.AsQueryable();
        var finesQuery = _context.Fines.AsQueryable();

        if (isMember)
        {
            borrowingsQuery = borrowingsQuery.Where(b => b.UserId == currentUserId);
            paymentsQuery = paymentsQuery.Where(p => p.Borrowing != null && p.Borrowing.UserId == currentUserId);
            finesQuery = finesQuery.Where(f => f.Borrowing != null && f.Borrowing.UserId == currentUserId);
        }

        var dashboard = new DashboardViewModel
        {
            BookCount = await _context.Books.CountAsync(b => !isMember || b.AvailabilityStatus == "Available"),
            AvailableBookCount = await _context.Books.CountAsync(b => b.AvailabilityStatus == "Available"),
            MemberCount = isMember ? 1 : await _context.Users.CountAsync(u => u.Role != null && u.Role.RoleName == "Member"),
            ActiveBorrowingCount = await borrowingsQuery.CountAsync(b => b.Status == "Borrowed"),
            OverdueBorrowingCount = await borrowingsQuery.CountAsync(b => b.Status == "Borrowed" && b.DueDate < DateTime.Today),
            PaymentCount = await paymentsQuery.CountAsync(),
            UnpaidFineCount = await finesQuery.CountAsync(f => f.Status == "Unpaid"),
            TotalPaidAmount = await paymentsQuery
                .Where(p => p.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            RecentBorrowings = await borrowingsQuery
                .Include(b => b.Book)
                .Include(b => b.User)
                .OrderByDescending(b => b.BorrowDate)
                .Take(5)
                .ToListAsync(),
            RecentPayments = await paymentsQuery
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.User)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync()
        };

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
