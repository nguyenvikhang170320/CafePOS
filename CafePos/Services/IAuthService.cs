using CafePos.Models;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public interface IAuthService
    {
        Task<User> LoginAsync(string username, string password);
        Task<User> RegisterAsync(User user, string password);
    }
}