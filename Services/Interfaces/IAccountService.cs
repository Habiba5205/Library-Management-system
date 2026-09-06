using Lib_System.Services;
using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IAccountService
    {
        Task<AccountLoginResult> LoginAsync(LoginViewModel vm);
        Task<ServiceResult> RegisterMemberAsync(RegisterViewModel vm);
    }
}
