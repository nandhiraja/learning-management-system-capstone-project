using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IUserRepository : IRepository<int, User>
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserWithRoleAsync(int id);
    }
}
