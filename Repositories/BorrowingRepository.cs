using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class BorrowingRepository : IBorrowingRepository
    {
        private readonly ApplicationDbContext _context;

        public BorrowingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Borrowing>> GetAllAsync(int? restrictToUserId, string? status)
        {
            var query = _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .AsQueryable();

            if (restrictToUserId.HasValue)
            {
                query = query.Where(b => b.UserId == restrictToUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            return await query
                .OrderByDescending(b => b.BorrowingId)
                .ToListAsync();
        }

        public async Task<Borrowing?> GetByIdAsync(int id)
        {
            return await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book!.Category)
                .Include(b => b.Book)
                    .ThenInclude(book => book!.BookAuthors)
                    .ThenInclude(bookAuthor => bookAuthor.Author)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Include(b => b.Fines)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);
        }

        public async Task<Borrowing?> GetForReturnAsync(int id)
        {
            return await _context.Borrowings
                .Include(b => b.Fines)
                .Include(b => b.Book)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);
        }

        public async Task AddAsync(Borrowing borrowing)
        {
            await _context.Borrowings.AddAsync(borrowing);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Borrowings.AnyAsync(b => b.BorrowingId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
