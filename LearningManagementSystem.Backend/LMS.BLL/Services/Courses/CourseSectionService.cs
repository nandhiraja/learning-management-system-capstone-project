using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.Core.Exception;
using LMS.Core.Enums;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class CourseSectionService : ICourseSectionService
    {
        private readonly ICourseSectionRepository _sectionRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public CourseSectionService(
            ICourseSectionRepository sectionRepository,
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _sectionRepository = sectionRepository;
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<CourseSectionResponse> CreateSectionAsync(Guid courseGuid, CourseSectionRequest request, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
            {
                throw new NotFoundException("Course", courseGuid);
            }

            // Admins and other users cannot add sections. Only the owning Instructor can.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to add sections to it.");
            }

            var section = new CourseSection
            {
                Title = request.Title,
                Order = request.Order,
                Description = string.Empty, // Default empty
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdSection = await _sectionRepository.Create(section);

            if (course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            return _mapper.Map<CourseSectionResponse>(createdSection);
        }

        public async Task<IEnumerable<CourseSectionResponse>> GetSectionsByCourseIdAsync(Guid courseGuid)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
            {
                throw new NotFoundException("Course", courseGuid);
            }

            var sections = await _sectionRepository.GetSectionsByCourseIdAsync(course.Id);
            var responseList = _mapper.Map<IEnumerable<CourseSectionResponse>>(sections);

            // Order sections by display order
            return responseList.OrderBy(s => s.Order);
        }

        public async Task<bool> UpdateSectionAsync(int sectionId, CourseSectionRequest request, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var section = await _sectionRepository.Get(sectionId);
            if (section == null) return false;

            var course = await _courseRepository.Get(section.CourseId);
            if (course == null) return false;

            // Admins and other users cannot edit sections. Only the owning Instructor can.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to update sections in it.");
            }

            section.Title = request.Title;
            section.Order = request.Order;
            section.UpdatedAt = DateTime.UtcNow;

            await _sectionRepository.Update(section);

            if (course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            return true;
        }

        public async Task<bool> DeleteSectionAsync(int sectionId, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var section = await _sectionRepository.Get(sectionId);
            if (section == null) return false;

            var course = await _courseRepository.GetCourseWithDetailsAsync(section.CourseId);
            if (course == null) return false;

            // Only the owning Instructor can delete sections. Admins cannot edit/delete sections directly.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to delete sections from it.");
            }

            // Cannot delete if there are active learners
            bool hasActiveEnrollments = course.Enrollments != null &&
                                       course.Enrollments.Any(e => e.Status == EnrollmentStatus.Active);

            if (hasActiveEnrollments)
            {
                throw new InvalidOperationException("Cannot delete course materials while there are active learners in the course.");
            }

            await _sectionRepository.Delete(section);

            if (course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            return true;
        }
    }
}
