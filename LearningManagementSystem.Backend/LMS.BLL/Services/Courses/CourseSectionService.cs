using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.Core.Exception;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class CourseSectionService : ICourseSectionService
    {
        private readonly ICourseSectionRepository _sectionRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IMapper _mapper;

        public CourseSectionService(
            ICourseSectionRepository sectionRepository,
            ICourseRepository courseRepository,
            IMapper mapper)
        {
            _sectionRepository = sectionRepository;
            _courseRepository = courseRepository;
            _mapper = mapper;
        }

        public async Task<CourseSectionResponse> CreateSectionAsync(Guid courseGuid, CourseSectionRequest request)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
            {
                throw new NotFoundException("Course", courseGuid);
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

        public async Task<bool> UpdateSectionAsync(int sectionId, CourseSectionRequest request)
        {
            var section = await _sectionRepository.Get(sectionId);
            if (section == null) return false;

            section.Title = request.Title;
            section.Order = request.Order;
            section.UpdatedAt = DateTime.UtcNow;

            await _sectionRepository.Update(section);
            return true;
        }

        public async Task<bool> DeleteSectionAsync(int sectionId)
        {
            var section = await _sectionRepository.Get(sectionId);
            if (section == null) return false;

            await _sectionRepository.Delete(section);
            return true;
        }
    }
}
