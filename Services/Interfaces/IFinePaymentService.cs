using Lib_System.Services;

namespace Lib_System.Services.Interfaces;

/// <summary>
/// Business workflow for paying off fines and returning books once fines are
/// settled (or there were none to begin with).
/// </summary>
public interface IFinePaymentService
{
    Task<ServiceResult> StartAsync(int id, int memberId, string method, decimal amount);

    Task<ServiceResult> CompleteAsync(int id, int? memberId, string method,
        Guid attemptId, decimal amount, bool success);

    Task<ServiceResult> ReturnWithoutFineAsync(int borrowingId);
}
