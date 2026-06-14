using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class UserRepository : Repository<Guid, User>, IUserRepository
    {
        public UserRepository(LMSDBContext context) : base(context)
        {
        }

        public override async Task<User?> Get(Guid k)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.ExternalId == k);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserWithRoleAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetBlockedUsersCountAsync()
        {
            return await _context.Users.CountAsync(u => !u.IsActive);
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
        }

        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
        }
    }
}
