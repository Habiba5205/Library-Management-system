using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;

namespace Lib_System.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IBorrowingRepository _borrowingRepository;

        public PaymentService(IPaymentRepository paymentRepository, IBorrowingRepository borrowingRepository)
        {
            _paymentRepository = paymentRepository;
            _borrowingRepository = borrowingRepository;
        }

        public Task<List<Payment>> GetAllAsync(int? restrictToUserId, string? status)
        {
            return _paymentRepository.GetAllAsync(restrictToUserId, status);
        }

        public Task<Payment?> GetDetailsAsync(int id) => _paymentRepository.GetByIdWithDetailsAsync(id);

        public Task<Payment?> GetForEditAsync(int id) => _paymentRepository.GetByIdAsync(id);

        public Task<Payment?> GetForDeleteAsync(int id) => _paymentRepository.GetByIdWithDetailsAsync(id);

        public async Task<ServiceResult> ValidateBorrowingAsync(int borrowingId)
        {
            var result = new ServiceResult();

            if (!await _borrowingRepository.ExistsAsync(borrowingId))
            {
                result.AddError("BorrowingId", "Select a valid borrowing record.");
            }

            return result;
        }

        public async Task CreateAsync(PaymentFormViewModel vm)
        {
            var payment = new Payment
            {
                BorrowingId = vm.BorrowingId,
                Amount = vm.Amount,
                PaymentDate = vm.PaymentDate,
                PaymentMethod = vm.PaymentMethod,
                Status = vm.Status,
                TransactionReference = vm.TransactionReference
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(int id, PaymentFormViewModel vm)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null) return false;

            payment.Amount = vm.Amount;
            payment.PaymentDate = vm.PaymentDate;
            payment.Status = vm.Status;
            payment.TransactionReference = vm.TransactionReference;

            await _paymentRepository.SaveChangesAsync();
            return true;
        }

        public Task<bool> ExistsAsync(int id) => _paymentRepository.ExistsAsync(id);

        public async Task DeleteAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment != null)
            {
                await _paymentRepository.RemoveAsync(payment);
                await _paymentRepository.SaveChangesAsync();
            }
        }

        public Task<List<Borrowing>> GetBorrowingsForDropdownAsync() => _borrowingRepository.GetAllAsync(null, null);
    }
}
