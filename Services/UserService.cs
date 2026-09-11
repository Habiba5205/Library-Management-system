using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Lib_System.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public Task<List<User>> GetAllAsync() => _userRepository.GetAllAsync();

        public Task<User?> GetDetailsAsync(int id) => _userRepository.GetByIdWithDetailsAsync(id);

        public Task<User?> GetForEditAsync(int id) => _userRepository.GetByIdAsync(id);

        public Task<User?> GetForStatusChangeAsync(int id) => _userRepository.GetByIdWithRelationsAsync(id);

        public async Task<ServiceResult> ValidateForCreateAsync(UserFormViewModel vm)
        {
            var result = new ServiceResult();

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                result.AddError("Password", "Password is required.");
            }

            if (await _userRepository.EmailExistsAsync(vm.Email, null))
            {
                result.AddError("Email", "This email is already registered.");
            }

            if (await _userRepository.UsernameExistsAsync(vm.Username, null))
            {
                result.AddError("Username", "This username is already taken.");
            }

            return result;
        }

        public async Task CreateAsync(UserFormViewModel vm)
        {
            var user = new User
            {
                Name = vm.Name,
                Email = vm.Email,
                Username = vm.Username,
                Phone = vm.Phone,
                Address = vm.Address,
                Status = "Active",
                RoleId = vm.RoleId,
                RegistrationDate = DateTime.Today,
                CreatedDate = DateTime.Now
            };

            // Plaintext password from the form is hashed here and discarded -
            // only the hash is stored.
            user.PasswordHash = _passwordHasher.HashPassword(user, vm.Password!);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<ServiceResult> ValidateForEditAsync(int id, UserFormViewModel vm)
        {
            var result = new ServiceResult();

            if (await _userRepository.UsernameExistsAsync(vm.Username, id))
            {
                result.AddError("Username", "This username is already taken.");
            }

            return result;
        }

        public async Task<bool> UpdateAsync(int id, UserFormViewModel vm)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.Name = vm.Name;
            user.Username = vm.Username;
            user.Phone = vm.Phone;
            user.Address = vm.Address;
            user.RoleId = vm.RoleId;
            // Email is deliberately not editable here - see Views/User/Edit.cshtml.
            // If a password is provided on edit, hash and update it; an empty
            // password means keep the current password.
            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, vm.Password);
            }

            await _userRepository.SaveChangesAsync();
            return true;
        }

        public Task<bool> ExistsAsync(int id) => _userRepository.ExistsAsync(id);

        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Active", "Deactivated", "Suspended" };

        public async Task<ServiceResult> SetStatusAsync(int id, string status)
        {
            var result = new ServiceResult();

            if (!ValidStatuses.Contains(status))
            {
                result.AddError("Status must be Active, Deactivated, or Suspended.");
                return result;
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                result.AddError("User not found.");
                return result;
            }

            // No borrowing/payment/fine-history check needed here (unlike the
            // old hard delete) - a status change never removes the row, so
            // that history always stays attached to a real user.
            user.Status = status;
            await _userRepository.SaveChangesAsync();
            return result;
        }

        public Task<List<Role>> GetRolesAsync() => _roleRepository.GetAllAsync();
    }
}