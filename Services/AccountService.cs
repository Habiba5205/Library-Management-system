using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Lib_System.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountService(IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<AccountLoginResult> LoginAsync(LoginViewModel vm)
        {
            var result = new AccountLoginResult();
            var user = await _userRepository.GetActiveByUsernameOrEmailAsync(vm.UsernameOrEmail);

            if (user == null ||
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
            {
                result.AddError("The username, email address, or password is incorrect.");
                return result;
            }

            result.User = user;
            return result;
        }

        public async Task<ServiceResult> RegisterMemberAsync(RegisterViewModel vm)
        {
            var result = new ServiceResult();

            if (await _userRepository.EmailExistsAsync(vm.Email, null))
            {
                result.AddError(nameof(vm.Email), "This email is already registered.");
            }

            if (await _userRepository.UsernameExistsAsync(vm.Username, null))
            {
                result.AddError(nameof(vm.Username), "This username is already registered.");
            }

            var memberRole = await _roleRepository.GetByNameAsync("Member");
            if (memberRole == null)
            {
                result.AddError("Member role was not found.");
            }

            if (!result.Success)
            {
                return result;
            }

            var user = new User
            {
                Name = vm.Name,
                Email = vm.Email,
                Username = vm.Username,
                Phone = vm.Phone,
                Address = vm.Address,
                RoleId = memberRole!.RoleId,
                Status = "Active",
                RegistrationDate = DateTime.Today,
                CreatedDate = DateTime.Now
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, vm.Password);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return result;
        }
    }
}
