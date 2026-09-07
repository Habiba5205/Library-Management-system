using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class FineRepository : IFineRepository
    {
        private readonly ApplicationDbContext _context;

        public FineRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Fine>> GetAllAsync(int? restrictToUserId, string? status)
        {
            var query = _context.Fines
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.User)
                .AsQueryable();

            if (restrictToUserId.HasValue)
            {
                query = query.Where(f => f.Borrowing != null && f.Borrowing.UserId == restrictToUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            return await query
                .OrderByDescending(f => f.FineId)
                .ToListAsync();
        }

        public async Task<Fine?> GetByIdAsync(int id)
        {
            return await _context.Fines.FindAsync(id);
        }

        public async Task<Fine?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Fines
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Fines)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(f => f.FineId == id);
        }

        public async Task AddAsync(Fine fine)
        {
            await _context.Fines.AddAsync(fine);
        }

        public async Task RemoveAsync(Fine fine)
        {
            _context.Fines.Remove(fine);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Fines.AnyAsync(f => f.FineId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
