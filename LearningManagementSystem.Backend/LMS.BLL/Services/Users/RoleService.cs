using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            return await _roleRepository.GetAllAsync();
        }

        public async Task<Role> CreateRoleAsync(string name)
        {
            var role = new Role { Name = name };
            return await _roleRepository.Create(role);
        }

        public async Task<bool> UpdateRoleAsync(int roleId, string name)
        {
            var role = await _roleRepository.Get(roleId);
            if (role == null) return false;

            role.Name = name;
            await _roleRepository.Update(role);
            return true;
        }

        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            var role = await _roleRepository.Get(roleId);
            if (role == null) return false;

            await _roleRepository.Delete(role);
            return true;
        }
    }
}
