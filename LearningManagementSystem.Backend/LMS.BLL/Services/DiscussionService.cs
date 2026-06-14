using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Exception;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class DiscussionService : IDiscussionService
    {
        private readonly IDiscussionRepository _discussionRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILectureRepository _lectureRepository;
        private readonly IMapper _mapper;

        public DiscussionService(
            IDiscussionRepository discussionRepository,
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            IEnrollmentRepository enrollmentRepository,
            ILectureRepository lectureRepository,
            IMapper mapper)
        {
            _discussionRepository = discussionRepository;
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _enrollmentRepository = enrollmentRepository;
            _lectureRepository = lectureRepository;
            _mapper = mapper;
        }

        private async Task ValidateUserAccessAsync(Course course, User user)
        {
            bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isInstructor = course.InstructorId == user.Id;

            if (isAdmin || isInstructor)
                return;

            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            bool isEnrolled = enrollments.Any(e => e.CourseId == course.Id && 
                (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));

            if (!isEnrolled)
            {
                throw new UnauthorizedAccessException("Only enrolled students, course instructor, or administrators can access the discussions.");
            }
        }

        public async Task<DiscussionResponse> CreateDiscussionAsync(Guid courseGuid, Guid userGuid, DiscussionCreateRequest request)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            await ValidateUserAccessAsync(course, user);

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(request.LectureId);
            if (lecture == null)
                throw new NotFoundException(nameof(Lecture), request.LectureId);

            if (lecture.CourseSection?.Course?.Id != course.Id)
            {
                throw new InvalidOperationException("The specified lecture does not belong to this course.");
            }

            var discussion = new Discussion
            {
                CourseId = course.Id,
                UserId = user.Id,
                LectureId = request.LectureId,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _discussionRepository.Create(discussion);
            created.User = user;
            return _mapper.Map<DiscussionResponse>(created);
        }

        public async Task<IEnumerable<DiscussionResponse>> GetDiscussionsForCourseAsync(Guid courseGuid, Guid userGuid, int? lectureId = null)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            await ValidateUserAccessAsync(course, user);

            var discussions = await _discussionRepository.GetDiscussionsByCourseIdAsync(course.Id);

            if (lectureId.HasValue)
            {
                discussions = discussions.Where(d => d.LectureId == lectureId.Value).ToList();
            }

            return _mapper.Map<IEnumerable<DiscussionResponse>>(discussions);
        }

        public async Task<DiscussionDetailResponse?> GetDiscussionDetailsAsync(Guid discussionGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var discussion = await _discussionRepository.GetDiscussionWithRepliesAsync(discussionGuid);
            if (discussion == null) return null;

            var course = await _courseRepository.GetCourseWithDetailsAsync(discussion.CourseId);
            if (course == null)
                throw new NotFoundException(nameof(Course), discussion.CourseId);

            await ValidateUserAccessAsync(course, user);

            var resp = _mapper.Map<DiscussionDetailResponse>(discussion);
            
            // Map and sort the replies (pinned replies first, then chronological) and set programmatic helper flags:
            if (discussion.Replies != null)
            {
                var sortedReplies = discussion.Replies
                    .OrderByDescending(r => r.IsPinned)
                    .ThenBy(r => r.CreatedAt)
                    .ToList();

                var respRepliesList = _mapper.Map<List<DiscussionReplyResponse>>(sortedReplies);

                for (int i = 0; i < sortedReplies.Count; i++)
                {
                    var reply = sortedReplies[i];
                    var respReply = respRepliesList[i];

                    respReply.IsAuthorReply = (reply.UserId == discussion.UserId);
                    respReply.IsInstructorReply = (reply.User?.Role?.Name?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) ?? false) 
                                                 || (reply.UserId == course.InstructorId);
                }

                resp.Replies = respRepliesList;
            }

            return resp;
        }

        public async Task<DiscussionReplyResponse> CreateReplyAsync(Guid discussionGuid, Guid userGuid, DiscussionReplyCreateRequest request)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var discussion = await _discussionRepository.GetByExternalIdAsync(discussionGuid);
            if (discussion == null)
                throw new NotFoundException(nameof(Discussion), discussionGuid);

            var course = await _courseRepository.GetCourseWithDetailsAsync(discussion.CourseId);
            if (course == null)
                throw new NotFoundException(nameof(Course), discussion.CourseId);

            await ValidateUserAccessAsync(course, user);

            var reply = new DiscussionReply
            {
                DiscussionId = discussion.Id,
                UserId = user.Id,
                Content = request.Content,
                IsPinned = false,
                LikesCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            var createdReply = await _discussionRepository.CreateReplyAsync(reply);
            createdReply.User = user;

            // Update discussion thread Update time
            discussion.UpdatedAt = DateTime.UtcNow;
            await _discussionRepository.Update(discussion);

            var resp = _mapper.Map<DiscussionReplyResponse>(createdReply);
            resp.IsAuthorReply = (createdReply.UserId == discussion.UserId);
            resp.IsInstructorReply = (user.Role?.Name?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) ?? false) 
                                     || (createdReply.UserId == course.InstructorId);

            return resp;
        }

        public async Task<bool> TogglePinReplyAsync(Guid replyGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var reply = await _discussionRepository.GetReplyByExternalIdAsync(replyGuid);
            if (reply == null)
                throw new NotFoundException(nameof(DiscussionReply), replyGuid);

            var discussion = await _discussionRepository.Get(reply.DiscussionId);
            if (discussion == null)
                throw new NotFoundException(nameof(Discussion), reply.DiscussionId);

            var course = await _courseRepository.Get(discussion.CourseId);
            if (course == null)
                throw new NotFoundException(nameof(Course), discussion.CourseId);

            bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isInstructor = course.InstructorId == user.Id;

            if (!isAdmin && !isInstructor)
            {
                throw new UnauthorizedAccessException("Only the course instructor or an administrator can pin/unpin replies.");
            }

            reply.IsPinned = !reply.IsPinned;
            await _discussionRepository.UpdateReplyAsync(reply);
            return reply.IsPinned;
        }

        public async Task<int> LikeReplyAsync(Guid replyGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var reply = await _discussionRepository.GetReplyByExternalIdAsync(replyGuid);
            if (reply == null)
                throw new NotFoundException(nameof(DiscussionReply), replyGuid);

            var discussion = await _discussionRepository.Get(reply.DiscussionId);
            if (discussion == null)
                throw new NotFoundException(nameof(Discussion), reply.DiscussionId);

            var course = await _courseRepository.GetCourseWithDetailsAsync(discussion.CourseId);
            if (course == null)
                throw new NotFoundException(nameof(Course), discussion.CourseId);

            await ValidateUserAccessAsync(course, user);

            reply.LikesCount += 1;
            await _discussionRepository.UpdateReplyAsync(reply);
            return reply.LikesCount;
        }
    }
}
