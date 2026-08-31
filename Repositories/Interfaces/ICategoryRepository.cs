using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetByIdWithBooksAsync(int id);
        Task AddAsync(Category category);
        void Update(Category category);
        Task RemoveAsync(Category category);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}