using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Exception;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class CourseReviewService : ICourseReviewService
    {
        private readonly ICourseReviewRepository _courseReviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IMapper _mapper;

        public CourseReviewService(
            ICourseReviewRepository courseReviewRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IMapper mapper)
        {
            _courseReviewRepository = courseReviewRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _mapper = mapper;
        }

        public async Task<ReviewResponse> AddReviewAsync(Guid courseGuid, Guid userGuid, ReviewRequest request)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            if (!enrollments.Any(e => e.CourseId == course.Id))
                throw new InvalidOperationException("Only enrolled users can review this course.");

            var review = new CourseReview
            {
                CourseId = course.Id,
                UserId = user.Id,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            var createdReview = await _courseReviewRepository.Create(review);
            createdReview.User = user; // Associate User object to support AutoMapper mapping of UserName

            return _mapper.Map<ReviewResponse>(createdReview);
        }

        public async Task<IEnumerable<ReviewResponse>> GetReviewsByCourseAsync(Guid courseGuid)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            var reviews = await _courseReviewRepository.GetReviewsByCourseIdAsync(course.Id);
            return _mapper.Map<IEnumerable<ReviewResponse>>(reviews);
        }

        public async Task<bool> UpdateReviewAsync(int reviewId, Guid userGuid, ReviewRequest request)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var review = await _courseReviewRepository.Get(reviewId);
            if (review == null) return false;

            if (review.UserId != user.Id)
                throw new UnauthorizedAccessException("You are not authorized to update this review.");

            review.Rating = request.Rating;
            review.Comment = request.Comment;

            await _courseReviewRepository.Update(review);
            return true;
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var review = await _courseReviewRepository.Get(reviewId);
            if (review == null) return false;

            if (review.UserId != user.Id)
                throw new UnauthorizedAccessException("You are not authorized to delete this review.");

            await _courseReviewRepository.Delete(review);
            return true;
        }
    }
}
