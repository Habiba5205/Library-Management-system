using Lib_System.Models;
using Lib_System.Repositories;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;

namespace Lib_System.Services;

public class PaymentService(IPaymentRepository payments, PaymentWorkflowRepository workflow) : IPaymentService
{
    public async Task<List<Payment>> GetAllAsync(int? restrictToUserId, string? status)
    {
        await workflow.ExpireAsync();
        return await payments.GetAllAsync(restrictToUserId, status);
    }

    public async Task<Payment?> GetDetailsAsync(int id)
    {
        await workflow.ExpireAsync();
        return await payments.GetByIdWithDetailsAsync(id);
    }

    public Task<bool> ConfirmCashAsync(int id) => workflow.CompleteAsync(id, "Cash", null, true);

    public Task<bool> CompleteDemoAsync(int id, int memberId, bool success) =>
        workflow.CompleteAsync(id, "Card", memberId, success);
}
