using Lib_System.Models;

namespace Lib_System.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllAsync();
    }
}