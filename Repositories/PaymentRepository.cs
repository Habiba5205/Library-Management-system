using Lib_System.Data;
using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Payment>> GetAllAsync(int? restrictToUserId, string? status)
        {
            var query = _context.Payments
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.User)
                .AsQueryable();

            if (restrictToUserId.HasValue)
            {
                query = query.Where(p => p.Borrowing != null && p.Borrowing.UserId == restrictToUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            return await query
                .OrderByDescending(p => p.PaymentId)
                .ToListAsync();
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments.FindAsync(id);
        }

        public async Task<Payment?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task RemoveAsync(Payment payment)
        {
            _context.Payments.Remove(payment);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Payments.AnyAsync(p => p.PaymentId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
