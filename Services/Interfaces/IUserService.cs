using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetDetailsAsync(int id);
        Task<User?> GetForEditAsync(int id);
        Task<User?> GetForStatusChangeAsync(int id);

        Task<ServiceResult> ValidateForCreateAsync(UserFormViewModel vm);
        Task CreateAsync(UserFormViewModel vm);

        Task<ServiceResult> ValidateForEditAsync(int id, UserFormViewModel vm);
        Task<bool> UpdateAsync(int id, UserFormViewModel vm);

        Task<bool> ExistsAsync(int id);

        /// <summary>Admin-only. Users are never hard-deleted - this flips Status
        /// between "Active", "Deactivated" and "Suspended" instead, which also
        /// blocks login (see IUserRepository.GetActiveByUsernameOrEmailAsync)
        /// while preserving all of their borrowing/payment/fine history.</summary>
        Task<ServiceResult> SetStatusAsync(int id, string status);

        Task<List<Role>> GetRolesAsync();
    }
}