using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<Category>> GetCategoriesAsync(bool onlyApproved = true)
        {
            var categories = await _categoryRepository.GetAllAsync();
            if (onlyApproved)
            {
                return categories.Where(c => c.IsApproved).ToList();
            }
            return categories;
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _categoryRepository.Get(id);
        }

        public async Task<Category> CreateCategoryAsync(string name, bool isApproved)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }
            name = name.Trim();

            var all = await _categoryRepository.GetAllAsync();
            if (all.Any(c => c.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Category '{name}' already exists.");
            }

            var category = new Category
            {
                Name = name,
                IsApproved = isApproved,
                CreatedAt = DateTime.UtcNow
            };
            return await _categoryRepository.Create(category);
        }

        public async Task<bool> UpdateCategoryAsync(int id, string name)
        {
            var category = await _categoryRepository.Get(id);
            if (category == null) return false;

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }
            name = name.Trim();

            var all = await _categoryRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Category '{name}' already exists.");
            }

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

        public async Task<bool> ApproveCategoryAsync(int id)
        {
            var category = await _categoryRepository.Get(id);
            if (category == null) return false;

            category.IsApproved = true;
            await _categoryRepository.Update(category);
            return true;
        }
    }
}
