using Lib_System.Models;
using Lib_System.Repositories;
using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;

namespace Lib_System.Services
{
    public class BorrowingService : IBorrowingService
    {
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly PaymentWorkflowRepository _workflow;
        private readonly OverdueFineRepository _overdueFines;
        private readonly FinePaymentRepository _finePayments;

        public BorrowingService(
            IBorrowingRepository borrowingRepository,
            IBookRepository bookRepository,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository, PaymentWorkflowRepository workflow, OverdueFineRepository overdueFines, FinePaymentRepository finePayments)
        {
            _borrowingRepository = borrowingRepository;
            _bookRepository = bookRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _workflow = workflow;
            _overdueFines = overdueFines;
            _finePayments = finePayments;
        }

        public Task<List<Borrowing>> GetBorrowingsAsync(int? restrictToUserId, string? status)
        {
            return _borrowingRepository.GetAllAsync(restrictToUserId, status);
        }

        public Task<Borrowing?> GetBorrowingDetailsAsync(int id) => _borrowingRepository.GetByIdAsync(id);

        public Task<Borrowing?> GetReturnCandidateAsync(int id) => _borrowingRepository.GetForReturnAsync(id);

        public async Task<ServiceResult> ValidateForCreateAsync(int bookId, int userId)
        {
            var result = new ServiceResult();

            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                result.AddError("BookId", "Selected book was not found.");
            }
            else if (book.AvailabilityStatus != "Available")
            {
                result.AddError("BookId", "This book is not available for borrowing.");
            }

            if (!await _userRepository.IsMemberAsync(userId))
            {
                result.AddError("UserId", "Select a valid library member.");
            }

            return result;
        }

        public Task<int?> CreateAsync(int bookId, int userId, string paymentMethod)
        {
            return _workflow.ReserveAsync(bookId, userId, paymentMethod);
        }

        public Task<ServiceResult> ReturnBorrowingAsync(int id) => _finePayments.ReturnWithoutFineAsync(id);

        public async Task<ServiceResult> RequestEarlyReturnAsync(int id)
        {
            var result = new ServiceResult();

            var borrowing = await _borrowingRepository.GetForReturnAsync(id);
            if (borrowing == null)
            {
                result.AddError("Borrowing not found.");
                return result;
            }

            if (borrowing.Status is not ("Borrowed" or "Early Return Requested"))
            {
                result.AddError("Only an active borrowing can be returned.");
                return result;
            }

            if (borrowing.Status == "Early Return Requested")
            {
                result.AddError("Early return was already requested.");
                return result;
            }

            if (DateTime.Today >= borrowing.DueDate.Date)
            {
                result.AddError("This borrowing is not before the due date anymore.");
                return result;
            }

            borrowing.Status = "Early Return Requested";

            await _borrowingRepository.SaveChangesAsync();
            return result;
        }

        public Task<List<Book>> GetAvailableBooksAsync() => _bookRepository.GetAvailableBooksAsync();
    }
}
