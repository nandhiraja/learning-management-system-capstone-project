using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _categoryRepository.Get(id);
        }

        public async Task<Category> CreateCategoryAsync(string name)
        {
            var category = new Category
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            return await _categoryRepository.Create(category);
        }

        public async Task<bool> UpdateCategoryAsync(int id, string name)
        {
            var category = await _categoryRepository.Get(id);
            if (category == null) return false;

            category.Name = name;
            await _categoryRepository.Update(category);
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepository.Get(id);
            if (category == null) return false;

            await _categoryRepository.Delete(category);
            return true;
        }
    }
}
