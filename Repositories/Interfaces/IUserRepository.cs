using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync(); // Include Role, ordered by Name

        Task<User?> GetByIdAsync(int id); // simple
        Task<User?> GetByIdWithDetailsAsync(int id);   // Role, ManagedBooks, Borrowings.Book (Details)
        Task<User?> GetByIdWithRelationsAsync(int id); // Role, Borrowings, ManagedBooks (Delete)
        Task<User?> GetActiveByUsernameOrEmailAsync(string usernameOrEmail);

        Task<bool> IsMemberAsync(int userId);
        Task<List<User>> GetMembersAsync(int? restrictToUserId);
        Task<List<User>> GetManagersAsync();

        Task<bool> EmailExistsAsync(string email, int? excludeUserId);
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId);

        Task AddAsync(User user);
        Task RemoveAsync(User user);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}
