using LMS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<Role>> GetRolesAsync();
        Task<Role> CreateRoleAsync(string name);
        Task<bool> UpdateRoleAsync(int roleId, string name);
        Task<bool> DeleteRoleAsync(int roleId);
    }
}
