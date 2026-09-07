using Lib_System.Models;

namespace Lib_System.Services.Interfaces
{
    
    public interface IBorrowingService
    {
        Task<List<Borrowing>> GetBorrowingsAsync(int? restrictToUserId, string? status);
        Task<Borrowing?> GetBorrowingDetailsAsync(int id);
        Task<Borrowing?> GetReturnCandidateAsync(int id);

        Task<ServiceResult> ValidateForCreateAsync(int bookId, int userId);
        Task<int?> CreateAsync(int bookId, int userId, string paymentMethod);

        Task<ServiceResult> RequestEarlyReturnAsync(int id);
        Task<ServiceResult> ReturnBorrowingAsync(int id);

        Task<List<Book>> GetAvailableBooksAsync();
    }
}
