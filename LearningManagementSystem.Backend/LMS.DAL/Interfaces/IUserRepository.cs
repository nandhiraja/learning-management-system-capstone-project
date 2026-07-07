using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IUserRepository : IRepository<Guid, User>
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserWithRoleAsync(int id);
        Task<int> GetCountAsync();
        Task<int> GetBlockedUsersCountAsync();
        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
        Task<List<User>> GetAllUsersWithRolesAsync();
    }
}
