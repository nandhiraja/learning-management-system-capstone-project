using LMS.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetCategoriesAsync(bool onlyApproved = true);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(string name, bool isApproved);
        Task<bool> UpdateCategoryAsync(int id, string name);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> ApproveCategoryAsync(int id);
    }
}
