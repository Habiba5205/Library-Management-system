using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Services;

public class OverdueFineService(ApplicationDbContext db, TimeProvider clock) : IOverdueFineService
{
    public const decimal DailyRate = 5m;

    public async Task SynchronizeAsync(int? returnedBorrowingId = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SynchronizeInTransactionAsync(returnedBorrowingId);
        await transaction.CommitAsync();
    }

    public async Task SynchronizeInTransactionAsync(int? returnedBorrowingId = null)
    {
        // A database lock coordinates the worker and concurrent web requests.
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = 'LibraryOverdueFines',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 51000, 'Could not acquire overdue fine lock.', 1;
            """);
        var today = clock.GetLocalNow().Date;
        var borrowings = await db.Borrowings.Include(b => b.Fines)
            .Where(b => b.DueDate < today &&
                (b.Status == "Borrowed" || b.Status == "Early Return Requested" ||
                (b.Status == "Returned" && (b.BorrowingId == returnedBorrowingId || b.Fines.Any(f => f.IsAutomatic)))))
            .ToListAsync();
        foreach (var borrowing in borrowings)
        {
            var endDate = borrowing.Status == "Returned" ? borrowing.ReturnDate?.Date : today;
            if (!endDate.HasValue) continue;
            var days = (endDate.Value - borrowing.DueDate.Date).Days;
            if (days <= 0) continue;
            var fine = borrowing.Fines.SingleOrDefault(f => f.IsAutomatic);
            if (fine == null)
            {
                fine = new Fine
                {
                    BorrowingId = borrowing.BorrowingId, IsAutomatic = true,
                    FineDate = borrowing.DueDate.Date.AddDays(1),
                    Reason = "Automatic overdue fine ($5 per day)", Status = "Unpaid"
                };
                db.Fines.Add(fine);
            }
            if (fine.Status == "Unpaid")
                fine.Amount = days * DailyRate;
        }
        await db.SaveChangesAsync();
    }
}
