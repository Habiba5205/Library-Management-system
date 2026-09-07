using Lib_System.Data;
using Lib_System.Repositories.Interfaces;
using Lib_System.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardAsync(bool isMember, int currentUserId)
        {
            var borrowingsQuery = _context.Borrowings.AsQueryable();
            var paymentsQuery = _context.Payments.AsQueryable();
            var finesQuery = _context.Fines.AsQueryable();

            if (isMember)
            {
                borrowingsQuery = borrowingsQuery.Where(b => b.UserId == currentUserId);
                paymentsQuery = paymentsQuery.Where(p => p.Borrowing != null && p.Borrowing.UserId == currentUserId);
                finesQuery = finesQuery.Where(f => f.Borrowing != null && f.Borrowing.UserId == currentUserId);
            }

            return new DashboardViewModel
            {
                BookCount = await _context.Books.CountAsync(b => !isMember || b.AvailabilityStatus == "Available"),
                AvailableBookCount = await _context.Books.CountAsync(b => b.AvailabilityStatus == "Available"),
                MemberCount = isMember ? 1 : await _context.Users.CountAsync(u => u.Role != null && u.Role.RoleName == "Member"),
                ActiveBorrowingCount = await borrowingsQuery.CountAsync(b => b.Status == "Borrowed" || b.Status == "Early Return Requested"),
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
        }
    }
}
