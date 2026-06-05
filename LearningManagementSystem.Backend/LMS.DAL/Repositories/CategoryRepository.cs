using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;

namespace LMS.DAL.Repositories
{
    public class CategoryRepository : Repository<int, Category>, ICategoryRepository
    {
        public CategoryRepository(LMSDBContext context) : base(context)
        {
        }
    }
}
