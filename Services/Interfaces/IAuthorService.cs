using Lib_System.Models;

namespace Lib_System.Services.Interfaces
{
    public interface IAuthorService
    {
        Task<List<Author>> GetAllAsync();
        Task<Author?> GetDetailsAsync(int id);
        Task<Author?> GetForEditAsync(int id);
        Task<Author?> GetForDeleteAsync(int id);
        Task CreateAsync(Author author);
        Task UpdateAsync(int id, Author author);
        Task<bool> ExistsAsync(int id);
        Task DeleteAsync(int id);
    }
}