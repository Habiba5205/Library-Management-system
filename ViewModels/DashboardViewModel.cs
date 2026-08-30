using Lib_System.Models;

namespace Lib_System.ViewModels
{
    public class DashboardViewModel
    {
        public int BookCount { get; set; }
        public int AvailableBookCount { get; set; }
        public int MemberCount { get; set; }
        public int ActiveBorrowingCount { get; set; }
        public int OverdueBorrowingCount { get; set; }
        public int PaymentCount { get; set; }
        public int UnpaidFineCount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public List<Borrowing> RecentBorrowings { get; set; } = new();
        public List<Payment> RecentPayments { get; set; } = new();
    }
}
