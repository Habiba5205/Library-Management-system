using System.Data;
using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Services;

public class PaymentWorkflowService(ApplicationDbContext db, TimeProvider clock) : IPaymentWorkflowService
{
    public async Task<int?> ReserveAsync(int bookId, int userId, string method)
    {
        if (method is not ("Cash" or "Card"))
            return null;

        await ExpireAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // The conditional update claims the book before another request can reserve it.
        var claimed = await db.Books.Where(b => b.BookId == bookId && b.AvailabilityStatus == "Available")
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.AvailabilityStatus, "Reserved"));
        if (claimed == 0) return null;
        if (!await db.Users.AnyAsync(u => u.UserId == userId && u.Status == "Active" && u.Role!.RoleName == "Member"))
            return null;

        var book = await db.Books.SingleAsync(b => b.BookId == bookId);
        await db.Entry(book).ReloadAsync();
        var now = clock.GetUtcNow().UtcDateTime;
        var borrowing = new Borrowing
        {
            BookId = bookId, UserId = userId, LoanDays = Borrowing.StandardLoanDays,
            BorrowDate = now.Date, DueDate = now.Date.AddDays(Borrowing.StandardLoanDays),
            Status = "Reserved",
            ReservationExpiresAtUtc = method == "Cash" ? now.AddDays(2) : now.AddMinutes(30)
        };
        var payment = new Payment
        {
            Borrowing = borrowing, Amount = book.Price, PaymentMethod = method,
            Status = "Pending", PaymentDate = now
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return payment.PaymentId;
    }

    public async Task<bool> CompleteAsync(int id, string method, int? memberId, bool success)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // Serialize payment completion against expiry and duplicate submissions.
        var payment = await db.Payments.FromSqlInterpolated(
                $"SELECT * FROM Payments WITH (UPDLOCK, ROWLOCK) WHERE PaymentId = {id}")
            .Include(p => p.Borrowing).ThenInclude(b => b!.Book).SingleOrDefaultAsync();
        if (payment == null || payment.PaymentMethod != method ||
            (method == "Card" && (!memberId.HasValue || payment.Borrowing!.UserId != memberId)))
            return false;
        if (payment.Status == "Paid") return success;
        var borrowing = payment.Borrowing!;
        if (payment.Status != "Pending" || borrowing.Status != "Reserved") return false;
        var now = clock.GetUtcNow().UtcDateTime;
        var expired = borrowing.ReservationExpiresAtUtc <= now;
        if (expired || !success)
        {
            Fail(payment);
        }
        else
        {
            payment.Status = "Paid";
            payment.PaymentDate = now;
            payment.TransactionReference = method == "Card" ? $"DEMO-{Guid.NewGuid():N}" : null;
            borrowing.Status = "Borrowed";
            borrowing.BorrowDate = now.Date;
            borrowing.LoanDays = Borrowing.StandardLoanDays;
            borrowing.DueDate = now.Date.AddDays(Borrowing.StandardLoanDays);
            borrowing.Book!.AvailabilityStatus = "Borrowed";
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return !expired;
    }

    public async Task ExpireAsync()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var ids = await db.Payments.AsNoTracking()
            .Where(p => p.Status == "Pending" && p.Borrowing!.Status == "Reserved" &&
                p.Borrowing.ReservationExpiresAtUtc <= now)
            .Select(p => new { p.PaymentId, p.PaymentMethod, p.Borrowing!.UserId }).ToListAsync();
        foreach (var item in ids)
            await CompleteAsync(item.PaymentId, item.PaymentMethod, item.UserId, false);
    }

    private static void Fail(Payment payment)
    {
        payment.Status = "Failed";
        payment.Borrowing!.Status = "Failed";
        payment.Borrowing.Book!.AvailabilityStatus = "Available";
    }
}
