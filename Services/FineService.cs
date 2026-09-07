using Lib_System.Models;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;

namespace Lib_System.Services
{
    public class FineService : IFineService
    {
        private readonly IFineRepository _fineRepository;
        private readonly IBorrowingRepository _borrowingRepository;

        public FineService(IFineRepository fineRepository, IBorrowingRepository borrowingRepository)
        {
            _fineRepository = fineRepository;
            _borrowingRepository = borrowingRepository;
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

        public async Task<bool> UpdateAsync(int id, FineFormViewModel vm)
        {
            var fine = await _fineRepository.GetByIdAsync(id);
            if (fine == null) return false;

            if (fine.IsAutomatic)
            {
                if (vm.Status is not ("Paid" or "Unpaid"))
                    throw new InvalidOperationException("Automatic fines can only be Unpaid or Paid.");
                var borrowing = await _borrowingRepository.GetByIdAsync(fine.BorrowingId);
                if (vm.Status == "Paid" && borrowing?.Status != "Returned")
                    throw new InvalidOperationException("Return the book before paying its automatic fine. The amount may still increase.");
                fine.Status = vm.Status;
                await _fineRepository.SaveChangesAsync();
                return true;
            }

            fine.BorrowingId = vm.BorrowingId;
            fine.Amount = vm.Amount;
            fine.FineDate = vm.FineDate;
            fine.Reason = vm.Reason;
            fine.Status = vm.Status;

            await _fineRepository.SaveChangesAsync();
            return true;
        }

        public Task<bool> ExistsAsync(int id) => _fineRepository.ExistsAsync(id);

        public async Task DeleteAsync(int id)
        {
            var fine = await _fineRepository.GetByIdAsync(id);
            if (fine != null)
            {
                if (fine.IsAutomatic)
                    throw new InvalidOperationException("Automatic overdue fines cannot be deleted.");
                await _fineRepository.RemoveAsync(fine);
                await _fineRepository.SaveChangesAsync();
            }
        }

        public Task<List<Borrowing>> GetBorrowingsForDropdownAsync() => _borrowingRepository.GetAllAsync(null, null);
    }
}
