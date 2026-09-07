using Lib_System.Models;

namespace Lib_System.Services.Interfaces;

public interface IPaymentService
{
    Task<List<Payment>> GetAllAsync(int? restrictToUserId, string? status);
    Task<Payment?> GetDetailsAsync(int id);
    Task<bool> ConfirmCashAsync(int id);
    Task<bool> CompleteDemoAsync(int id, int memberId, bool success);
}
