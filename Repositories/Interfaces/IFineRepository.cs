using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IFineRepository
    {
        Task<List<Fine>> GetAllAsync(int? restrictToUserId, string? status);
        Task<Fine?> GetByIdAsync(int id);
        Task<Fine?> GetByIdWithDetailsAsync(int id); // Borrowing.Book, Borrowing.User
        Task AddAsync(Fine fine);
        Task RemoveAsync(Fine fine);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}