using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace LMS.PL.Controllers
{
    [ApiController]
    [EnableRateLimiting("api-limiter")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("api/categories")]
        public async Task<IActionResult> GetApprovedCategories()
        {
            var categories = await _categoryService.GetCategoriesAsync(onlyApproved: true);
            return Ok(categories);
        }

        [HttpPost("api/categories")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            bool isApproved = User.IsInRole("Admin");
            var result = await _categoryService.CreateCategoryAsync(request.Name, isApproved);
            
            if (isApproved)
            {
                return Ok(new { category = result, message = "Category created and approved successfully" });
            }
            else
            {
                return Ok(new { category = result, message = "Category request submitted for Admin approval" });
            }
        }
    }

    public class CreateCategoryRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters.")]
        public string Name { get; set; } = null!;
    }
}
