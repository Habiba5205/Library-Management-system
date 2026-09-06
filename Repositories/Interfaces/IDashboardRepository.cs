using Lib_System.ViewModels;

namespace Lib_System.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardViewModel> GetDashboardAsync(bool isMember, int currentUserId);
    }
}
