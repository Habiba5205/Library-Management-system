using Lib_System.Data;
using Lib_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Services;

public class FinePaymentService(ApplicationDbContext db, IOverdueFineService overdue, TimeProvider clock)
    : IFinePaymentService
{
    public async Task<ServiceResult> StartAsync(int id, int memberId, string method, decimal amount)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.ChangeTracker.Clear();
        await overdue.SynchronizeInTransactionAsync();
        var fine = await db.Fines.Include(f => f.Borrowing).SingleOrDefaultAsync(f => f.FineId == id);
        var result = new ServiceResult();
        if (fine == null || fine.Borrowing!.UserId != memberId)
            result.AddError("Fine not found.");
        else if (fine.Status != "Unpaid")
            result.AddError("This fine is already settled.");
        else if (method is not ("Cash" or "Card"))
            result.AddError("Select Cash or Card.");
        else if (amount != fine.Amount)
            result.AddError("The fine amount has changed. Please confirm the updated amount.");
        else
        {
            fine.PaymentMethod = method;
            fine.PaymentAmount = fine.Amount;
            fine.PaymentAttemptId = Guid.NewGuid();
            fine.PaymentStatus = "Pending";
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return result;
    }

    public async Task<ServiceResult> CompleteAsync(int id, int? memberId, string method,
        Guid attemptId, decimal amount, bool success)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.ChangeTracker.Clear();
        await overdue.SynchronizeInTransactionAsync();
        var fine = await db.Fines.Include(f => f.Borrowing).ThenInclude(b => b!.Book)
            .Include(f => f.Borrowing).ThenInclude(b => b!.Fines).SingleOrDefaultAsync(f => f.FineId == id);
        var result = new ServiceResult();
        if (fine == null || (method == "Card" && fine.Borrowing!.UserId != memberId))
            result.AddError("Fine not found.");
        else if (fine.PaymentMethod != method || fine.PaymentAttemptId != attemptId)
            result.AddError("This payment attempt is no longer current.");
        else if (fine.Status == "Paid" && fine.PaymentStatus == "Paid")
        {
            // A repeated confirmation must not change the return date or charge again.
        }
        else if (fine.Status != "Unpaid" || fine.PaymentStatus != "Pending")
            result.AddError("This payment attempt is no longer pending.");
        else if (!success)
        {
            fine.PaymentStatus = "Failed";
        }
        else if (fine.IsAutomatic && fine.Borrowing!.Fines.Any(f => f.FineId != id && f.Status == "Unpaid"))
            result.AddError("Pay the other outstanding fines for this book before settling its overdue fine.");
        else if (fine.Amount != amount || fine.PaymentAmount != fine.Amount)
        {
            fine.PaymentStatus = "Failed";
            result.AddError("The fine has increased. The member must confirm the updated amount before paying.");
        }
        else
        {
            fine.Status = "Paid";
            fine.PaymentStatus = "Paid";
            fine.PaidAtUtc = clock.GetUtcNow().UtcDateTime;
            var borrowing = fine.Borrowing!;
            if ((borrowing.Status is "Borrowed" or "Early Return Requested") &&
                borrowing.Fines.All(f => f.Status != "Unpaid"))
            {
                borrowing.Status = "Returned";
                borrowing.ReturnDate = clock.GetLocalNow().Date;
                borrowing.Book!.AvailabilityStatus = "Available";
            }
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    public async Task<ServiceResult> ReturnWithoutFineAsync(int borrowingId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.ChangeTracker.Clear();
        await overdue.SynchronizeInTransactionAsync();
        var borrowing = await db.Borrowings.Include(b => b.Fines).Include(b => b.Book)
            .SingleOrDefaultAsync(b => b.BorrowingId == borrowingId);
        var result = new ServiceResult();
        if (borrowing == null || borrowing.Status is not ("Borrowed" or "Early Return Requested"))
            result.AddError("Only an active borrowing can be returned.");
        else if (borrowing.Fines.Any(f => f.Status == "Unpaid"))
            result.AddError("Pay the outstanding fines before returning this book.");
        else
        {
            borrowing.Status = "Returned";
            borrowing.ReturnDate = clock.GetLocalNow().Date;
            borrowing.Book!.AvailabilityStatus = "Available";
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return result;
    }
}
