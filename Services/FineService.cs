using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Repositories;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;

namespace Lib_System.Services
{
    public class FineService : IFineService
    {
        private readonly IFineRepository _fineRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly FinePaymentRepository _payments;

        public FineService(IFineRepository fineRepository, IBorrowingRepository borrowingRepository, FinePaymentRepository payments)
        {
            _fineRepository = fineRepository;
            _borrowingRepository = borrowingRepository;
            _payments = payments;
        }

        public Task<List<Fine>> GetAllAsync(int? restrictToUserId, string? status)
        {
            return _fineRepository.GetAllAsync(restrictToUserId, status);
        }

        public Task<Fine?> GetDetailsAsync(int id) => _fineRepository.GetByIdWithDetailsAsync(id);

        public Task<Fine?> GetForEditAsync(int id) => _fineRepository.GetByIdAsync(id);

        public Task<Fine?> GetForDeleteAsync(int id) => _fineRepository.GetByIdWithDetailsAsync(id);

        public async Task<ServiceResult> ValidateBorrowingAsync(int borrowingId)
        {
            var result = new ServiceResult();

            if (!await _borrowingRepository.ExistsAsync(borrowingId))
            {
                result.AddError("BorrowingId", "Select a valid borrowing record.");
            }

            return result;
        }

        public Task<bool> UpdateAsync(int id, FineFormViewModel vm) =>
            throw new InvalidOperationException("Use the fine payment flow to settle a fine.");

        public Task<bool> ExistsAsync(int id) => _fineRepository.ExistsAsync(id);

        public Task DeleteAsync(int id) =>
            throw new InvalidOperationException("Fine records cannot be deleted.");

        public Task<ServiceResult> StartPaymentAsync(int id, int memberId, string method, decimal amount) =>
            _payments.StartAsync(id, memberId, method, amount);

        public Task<ServiceResult> CompletePaymentAsync(int id, int? memberId, string method, Guid attemptId, decimal amount, bool success) =>
            _payments.CompleteAsync(id, memberId, method, attemptId, amount, success);

        public Task<List<Borrowing>> GetBorrowingsForDropdownAsync() => _borrowingRepository.GetAllAsync(null, null);
    }
}
