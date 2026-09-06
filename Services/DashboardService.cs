using Lib_System.Repositories.Interfaces;
using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;

namespace Lib_System.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardViewModel> GetDashboardAsync(bool isMember, int currentUserId)
        {
            return await _dashboardRepository.GetDashboardAsync(isMember, currentUserId);
        }
    }
}
