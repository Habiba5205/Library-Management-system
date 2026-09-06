using Lib_System.Models;

namespace Lib_System.Services
{
    public class AccountLoginResult : ServiceResult
    {
        public User? User { get; set; }
    }
}
