namespace Lib_System.Services.Interfaces;

/// <summary>
/// Keeps automatic overdue fines in sync with active/returned borrowings.
/// Business logic (fine calculation rules), not data access.
/// </summary>
public interface IOverdueFineService
{
    Task SynchronizeAsync(int? returnedBorrowingId = null);

    /// <summary>
    /// Same synchronization, but assumes the caller already owns an open
    /// transaction (used by other workflow services that need to run it as
    /// part of a larger unit of work).
    /// </summary>
    Task SynchronizeInTransactionAsync(int? returnedBorrowingId = null);
}
