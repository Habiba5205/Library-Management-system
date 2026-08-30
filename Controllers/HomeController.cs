using System.Diagnostics;
using Lib_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var dashboard = new DashboardViewModel
        {
            BookCount = await _context.Books.CountAsync(),
            AvailableBookCount = await _context.Books.CountAsync(b => b.AvailabilityStatus == "Available"),
            MemberCount = await _context.Users.CountAsync(u => u.Role != null && u.Role.RoleName == "Member"),
            ActiveBorrowingCount = await _context.Borrowings.CountAsync(b => b.Status == "Borrowed"),
            OverdueBorrowingCount = await _context.Borrowings.CountAsync(b => b.Status == "Borrowed" && b.DueDate < DateTime.Today),
            PaymentCount = await _context.Payments.CountAsync(),
            TotalPaidAmount = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            RecentBorrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .OrderByDescending(b => b.BorrowDate)
                .Take(5)
                .ToListAsync(),
            RecentPayments = await _context.Payments
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
