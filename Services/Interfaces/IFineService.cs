using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IFineService
    {
        Task<ServiceResult> StartPaymentAsync(int id, int memberId, string method, decimal amount);
        Task<ServiceResult> CompletePaymentAsync(int id, int? memberId, string method, Guid attemptId, decimal amount, bool success);
        Task<List<Fine>> GetAllAsync(int? restrictToUserId, string? status);
        Task<Fine?> GetDetailsAsync(int id);
        Task<Fine?> GetForEditAsync(int id);
        Task<Fine?> GetForDeleteAsync(int id);

        Task<ServiceResult> ValidateBorrowingAsync(int borrowingId);
        Task<bool> UpdateAsync(int id, FineFormViewModel vm);

        Task<bool> ExistsAsync(int id);
        Task DeleteAsync(int id);

        Task<List<Borrowing>> GetBorrowingsForDropdownAsync();
    }
}
