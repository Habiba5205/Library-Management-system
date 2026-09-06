using Lib_System.ViewModels;

namespace Lib_System.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(bool isMember, int currentUserId);
    }
}
