using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles.OrderByDescending(r => r.RoleId).ToListAsync();
        }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        }
    }
}
