using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetDetailsAsync(int id);
        Task<User?> GetForEditAsync(int id);
        Task<User?> GetForDeleteAsync(int id);

        Task<ServiceResult> ValidateForCreateAsync(UserFormViewModel vm);
        Task CreateAsync(UserFormViewModel vm);

        Task<ServiceResult> ValidateForEditAsync(int id, UserFormViewModel vm);
        Task<bool> UpdateAsync(int id, UserFormViewModel vm);

        Task<bool> ExistsAsync(int id);
        Task<ServiceResult> DeleteAsync(int id);

        Task<List<Role>> GetRolesAsync();
    }
}