using Microsoft.AspNetCore.Mvc;
using LMS.DAL.Data;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/database")]
    public class DatabaseController : ControllerBase
    {
        private readonly LMSDBContext _context;

        public DatabaseController(LMSDBContext context)
        {
            _context = context;
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            try
            {
                await DbSeeder.SeedDatabaseAsync(_context);
                return Ok(new
                {
                    Message = "Database cleared and seeded with rich mock data successfully.",
                    Users = new[]
                    {
                        new { Role = "Admin", Email = "admin@lms.com", Password = "Password123" },
                        new { Role = "Instructor (Primary)", Email = "john.doe@lms.com", Password = "Password123" },
                        new { Role = "Instructor (Secondary)", Email = "jane.smith@lms.com", Password = "Password123" },
                        new { Role = "Student (Enrolled)", Email = "student.active@lms.com", Password = "Password123" },
                        new { Role = "Student (New)", Email = "student.new@lms.com", Password = "Password123" }
                    }
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during database seeding.", Error = ex.Message });
            }
        }

        [HttpPost("clear")]
        public async Task<IActionResult> Clear()
        {
            try
            {
                await DbSeeder.ClearDatabaseAsync(_context);
                return Ok(new { Message = "All data cleared from database successfully." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during database clearing.", Error = ex.Message });
            }
        }
    }
}
