using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IBorrowingRepository
    {
        Task<List<Borrowing>> GetAllAsync(int? restrictToUserId, string? status);

        Task<Borrowing?> GetByIdAsync(int id);
        Task<Borrowing?> GetForReturnAsync(int id);

        Task AddAsync(Borrowing borrowing);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}