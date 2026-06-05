using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;

namespace LMS.DAL.Repositories
{
    public class RoleRepository : Repository<int, Role>, IRoleRepository
    {
        public RoleRepository(LMSDBContext context) : base(context)
        {
        }
    }
}
