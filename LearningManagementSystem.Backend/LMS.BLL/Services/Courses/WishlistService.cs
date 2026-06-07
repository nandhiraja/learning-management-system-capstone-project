using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class WishlistService : IWishlistService
    {
        // Thread-safe in-memory store for wishlists, indexed by User's ExternalId Guid
        private static readonly ConcurrentDictionary<Guid, List<int>> _wishlists = new ConcurrentDictionary<Guid, List<int>>();

        private readonly ICourseRepository _courseRepository;
        private readonly IMapper _mapper;

        public WishlistService(ICourseRepository courseRepository, IMapper mapper)
        {
            _courseRepository = courseRepository;
            _mapper = mapper;
        }

        public async Task<bool> AddToWishlistAsync(Guid userGuid, int courseId)
        {
            var course = await _courseRepository.Get(courseId);
            if (course == null) return false;

            var courseIds = _wishlists.GetOrAdd(userGuid, _ => new List<int>());
            lock (courseIds)
            {
                if (!courseIds.Contains(courseId))
                {
                    courseIds.Add(courseId);
                }
            }
            return true;
        }

        public async Task<IEnumerable<CourseResponse>> GetWishlistAsync(Guid userGuid)
        {
            var courseIds = _wishlists.GetOrAdd(userGuid, _ => new List<int>());
            var responseList = new List<CourseResponse>();

            foreach (var cid in courseIds)
            {
                var course = await _courseRepository.Get(cid);
                if (course != null)
                {
                    responseList.Add(_mapper.Map<CourseResponse>(course));
                }
            }

            return responseList;
        }

        public async Task<bool> RemoveFromWishlistAsync(Guid userGuid, int courseId)
        {
            var courseIds = _wishlists.GetOrAdd(userGuid, _ => new List<int>());
            lock (courseIds)
            {
                if (courseIds.Contains(courseId))
                {
                    courseIds.Remove(courseId);
                }
            }
            return true;
        }
    }
}
