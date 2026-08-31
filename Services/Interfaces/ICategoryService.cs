using Lib_System.Models;

namespace Lib_System.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetDetailsAsync(int id);
        Task<Category?> GetForEditAsync(int id);
        Task<Category?> GetForDeleteAsync(int id);
        Task CreateAsync(Category category);
        Task UpdateAsync(int id, Category category);
        Task<bool> ExistsAsync(int id);
        Task<ServiceResult> DeleteAsync(int id);
    }
}