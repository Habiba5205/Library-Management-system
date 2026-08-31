using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetAllAsync(int? restrictToUserId, string? status);
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByIdWithDetailsAsync(int id); // Borrowing.Book, Borrowing.User
        Task AddAsync(Payment payment);
        Task RemoveAsync(Payment payment);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}