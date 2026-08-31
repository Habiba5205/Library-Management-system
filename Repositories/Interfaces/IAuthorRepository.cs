using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IAuthorRepository
    {
        Task<List<Author>> GetAllAsync();
        Task<Author?> GetByIdAsync(int id);
        Task<Author?> GetByIdWithBooksAsync(int id);
        Task AddAsync(Author author);
        void Update(Author author);
        Task RemoveAsync(Author author);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}