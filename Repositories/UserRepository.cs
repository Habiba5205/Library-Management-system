using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.ManagedBooks)
                .Include(u => u.Borrowings)
                    .ThenInclude(b => b.Book)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetByIdWithRelationsAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Borrowings)
                .Include(u => u.ManagedBooks)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetActiveByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Status == "Active" &&
                    (u.Username == usernameOrEmail || u.Email == usernameOrEmail));
        }

        public async Task<bool> IsMemberAsync(int userId)
        {
            return await _context.Users
                .AnyAsync(u => u.UserId == userId && u.Role != null && u.Role.RoleName == "Member");
        }

        public async Task<List<User>> GetMembersAsync(int? restrictToUserId)
        {
            var query = _context.Users.Where(u => u.Role != null && u.Role.RoleName == "Member");

            if (restrictToUserId.HasValue)
            {
                query = query.Where(u => u.UserId == restrictToUserId.Value);
            }

            return await query.OrderBy(u => u.Name).ToListAsync();
        }

        public async Task<List<User>> GetManagersAsync()
        {
            return await _context.Users
                .Where(u => u.Role != null && u.Role.RoleName == "Manager")
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId)
        {
            var query = _context.Users.Where(u => u.Email == email);
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId)
        {
            var query = _context.Users.Where(u => u.Username == username);
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task RemoveAsync(User user)
        {
            _context.Users.Remove(user);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(u => u.UserId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
