using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetAllAsync(int? restrictToUserId, string? status);
        Task<Payment?> GetDetailsAsync(int id);
        Task<Payment?> GetForEditAsync(int id);
        Task<Payment?> GetForDeleteAsync(int id);

        Task<ServiceResult> ValidateBorrowingAsync(int borrowingId);
        Task CreateAsync(PaymentFormViewModel vm);
        Task<bool> UpdateAsync(int id, PaymentFormViewModel vm);

        Task<bool> ExistsAsync(int id);
        Task DeleteAsync(int id);

        Task<List<Borrowing>> GetBorrowingsForDropdownAsync();
    }
}