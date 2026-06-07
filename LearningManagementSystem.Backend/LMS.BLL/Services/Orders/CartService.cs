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
    public class CartService : ICartService
    {
        // Thread-safe in-memory store for carts, indexed by User's ExternalId Guid
        private static readonly ConcurrentDictionary<Guid, List<int>> _carts = new ConcurrentDictionary<Guid, List<int>>();

        private readonly ICourseRepository _courseRepository;
        private readonly IMapper _mapper;

        public CartService(ICourseRepository courseRepository, IMapper mapper)
        {
            _courseRepository = courseRepository;
            _mapper = mapper;
        }

        public async Task<CartResponse> GetCartAsync(Guid userGuid)
        {
            var courseIds = _carts.GetOrAdd(userGuid, _ => new List<int>());
            var itemsList = new List<CartItemResponse>();

            foreach (var cid in courseIds)
            {
                var course = await _courseRepository.Get(cid);
                if (course != null)
                {
                    itemsList.Add(new CartItemResponse
                    {
                        Id = course.Id,
                        Title = course.Title,
                        Price = course.Price
                    });
                }
            }

            return new CartResponse
            {
                Items = itemsList,
                Total = itemsList.Sum(i => i.Price)
            };
        }

        public async Task<bool> AddToCartAsync(Guid userGuid, int courseId)
        {
            var course = await _courseRepository.Get(courseId);
            if (course == null) return false;

            var courseIds = _carts.GetOrAdd(userGuid, _ => new List<int>());
            lock (courseIds)
            {
                if (!courseIds.Contains(courseId))
                {
                    courseIds.Add(courseId);
                }
            }
            return true;
        }

        public async Task<bool> RemoveFromCartAsync(Guid userGuid, int courseId)
        {
            var courseIds = _carts.GetOrAdd(userGuid, _ => new List<int>());
            lock (courseIds)
            {
                if (courseIds.Contains(courseId))
                {
                    courseIds.Remove(courseId);
                }
            }
            return true;
        }

        public async Task<bool> ClearCartAsync(Guid userGuid)
        {
            _carts.TryRemove(userGuid, out _);
            return await Task.FromResult(true);
        }
    }
}
