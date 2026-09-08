namespace Lib_System.Services.Interfaces;

/// <summary>
/// Business workflow for reserving a book against a payment and completing
/// (or expiring) that payment. This is business/transaction logic, not data
/// access, which is why it lives in Services rather than Repositories.
/// </summary>
public interface IPaymentWorkflowService
{
    Task<int?> ReserveAsync(int bookId, int userId, string method);
    Task<bool> CompleteAsync(int id, string method, int? memberId, bool success);
    Task ExpireAsync();
}
