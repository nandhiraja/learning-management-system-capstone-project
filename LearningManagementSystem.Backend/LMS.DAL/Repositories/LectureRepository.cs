using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class LectureRepository : Repository<int, Lecture>, ILectureRepository
    {
        public LectureRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Lecture>> GetLecturesBySectionIdAsync(int sectionId)
        {
            return await _context.Lectures
                .Include(l => l.Quizzes)
                .Where(l => l.CourseSectionId == sectionId)
                .ToListAsync();
        }

        public async Task<Lecture?> GetLectureWithDetailsAsync(int lectureId)
        {
            return await _context.Lectures
                .Include(l => l.Quizzes)
                .Include(l => l.CourseSection)
                    .ThenInclude(s => s.Course)
                        .ThenInclude(c => c.Enrollments)
                .Include(l => l.CourseSection)
                    .ThenInclude(s => s.Course)
                        .ThenInclude(c => c.Instructor)
                .FirstOrDefaultAsync(l => l.Id == lectureId);
        }

        public async Task<string> GetCombinedTranscriptTextAsync(int lectureId)
        {
            var segments = await _context.LectureTranscripts
                .Where(t => t.LectureId == lectureId)
                .OrderBy(t => t.StartTime)
                .Select(t => new { t.StartTime, t.Text })
                .ToListAsync();

            var formattedSegments = segments.Select(s =>
            {
                var time = TimeSpan.FromSeconds(s.StartTime);
                string timestamp = time.TotalHours >= 1 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
                return $"[{timestamp}] {s.Text}";
            });

            return string.Join(" ", formattedSegments);
        }

        public async Task<bool> HasTranscriptAsync(int lectureId)
        {
            return await _context.LectureTranscripts.AnyAsync(t => t.LectureId == lectureId);
        }
    }
}

