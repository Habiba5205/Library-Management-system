using Lib_System.Models;

namespace Lib_System.Services.Interfaces
{
    
    public interface IBorrowingService
    {
        Task<List<Borrowing>> GetBorrowingsAsync(int? restrictToUserId, string? status);
        Task<Borrowing?> GetBorrowingDetailsAsync(int id);
        Task<Borrowing?> GetReturnCandidateAsync(int id);

        Task<ServiceResult> ValidateForCreateAsync(int bookId, int userId);
        Task CreateAsync(int bookId, int userId, DateTime borrowDate, int loanDays);

        Task<ServiceResult> ReturnBorrowingAsync(int id);

        Task<List<Book>> GetAvailableBooksAsync();
        Task<List<User>> GetMembersAsync(int? restrictToUserId);
    }
}