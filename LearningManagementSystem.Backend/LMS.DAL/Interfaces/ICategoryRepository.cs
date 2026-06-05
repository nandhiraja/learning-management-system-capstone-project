using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Interfaces
{
    public interface ICategoryRepository : IRepository<int, Category>
    {
    }
}
