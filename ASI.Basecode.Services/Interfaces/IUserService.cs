using ASI.Basecode.Data.Models;
using System.Threading.Tasks;
namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        Task AuthenticateUser(User user);
        Task LogoutUser();
    }
}
