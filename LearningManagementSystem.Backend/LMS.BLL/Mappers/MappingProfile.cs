using System;
using AutoMapper;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Models;

namespace LMS.BLL.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, RegisterResponse>();
            CreateMap<User, UserEditResponse>();
            CreateMap<User, UserProfileResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty))
                .ForMember(dest => dest.CertificateName, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.CertificateName) ? src.CertificateName : ($"{src.FirstName} {src.LastName}").Trim()));
            CreateMap<RegisterRequest, User>();
            CreateMap<UserEditRequest, User>();

            // Course mappings
            CreateMap<Course, CourseResponse>()
                .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language != null ? src.Language.Name : string.Empty));
            CreateMap<CourseCreateRequest, Course>();
            CreateMap<CourseUpdateRequest, Course>();

            // Section mappings
            CreateMap<CourseSection, CourseSectionResponse>();
            CreateMap<CourseSectionRequest, CourseSection>();

            // Lecture mappings
            CreateMap<Lecture, LectureResponse>()
                .ForMember(dest => dest.QuizId, opt => opt.MapFrom(src => src.Quizzes != null && src.Quizzes.Any() ? src.Quizzes.First().Id : (int?)null))
                .AfterMap<LectureResponseSecureMediaMappingAction>();
            CreateMap<LectureRequest, Lecture>()
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => Enum.Parse<ContentType>(src.ContentType, true)));

            // Quiz mappings
            CreateMap<Quiz, QuizResponse>();
            CreateMap<QuizRequest, Quiz>();

            CreateMap<QuizQuestion, QuizQuestionResponse>();
            CreateMap<QuizQuestionRequest, QuizQuestion>();

            CreateMap<QuizOption, QuizOptionResponse>();
            CreateMap<QuizOptionRequest, QuizOption>();

            // Order mappings
            CreateMap<Order, OrderResponse>();

            // Payment mappings
            CreateMap<Payment, PaymentResponse>();
            CreateMap<PaymentCreateRequest, Payment>()
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => Enum.Parse<PaymentMethod>(src.PaymentMethod, true)));

            // Enrollment mappings 
            CreateMap<Enrollment, EnrollmentResponse>()
                .ForMember(dest => dest.CourseThumbnailUrl, opt => opt.MapFrom(src => src.Course != null ? src.Course.ThumbnailUrl : null))
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Course != null && src.Course.Instructor != null ? ($"{src.Course.Instructor.FirstName} {src.Course.Instructor.LastName}").Trim() : string.Empty));

            // Cart mappings
            CreateMap<Course, CartItemResponse>();

            // Progress mappings
            CreateMap<ProgressUpdateRequest, LectureProgress>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsCompleted ? LectureStatus.Completed : LectureStatus.InProgress));

            // Review mappings 
            CreateMap<CourseReview, ReviewResponse>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? ($"{src.User.FirstName} {src.User.LastName}").Trim() : string.Empty))
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.MapFrom(src => src.User != null ? src.User.ProfilePictureUrl : null));
            CreateMap<ReviewRequest, CourseReview>();

            // Certificate mappings
            CreateMap<Certificate, CertificateResponse>()
                .ForMember(dest => dest.UserGuid, opt => opt.MapFrom(src => src.User != null ? src.User.ExternalId : Guid.Empty))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RecipientFullName) ? src.RecipientFullName : (src.User != null ? ($"{src.User.FirstName} {src.User.LastName}").Trim() : string.Empty)))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.CourseGuid, opt => opt.MapFrom(src => src.Course != null ? src.Course.ExternalId : Guid.Empty))
                .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course != null ? src.Course.Title : string.Empty))
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Course != null && src.Course.Instructor != null ? ($"{src.Course.Instructor.FirstName} {src.Course.Instructor.LastName}").Trim() : string.Empty));

            // Discussion mappings
            CreateMap<Discussion, DiscussionResponse>()
                .ForMember(dest => dest.RepliesCount, opt => opt.MapFrom(src => src.Replies != null ? src.Replies.Count : 0))
                .ForMember(dest => dest.LectureTitle, opt => opt.MapFrom(src => src.Lecture != null ? src.Lecture.Title : null));
            CreateMap<Discussion, DiscussionDetailResponse>()
                .ForMember(dest => dest.LectureTitle, opt => opt.MapFrom(src => src.Lecture != null ? src.Lecture.Title : null));
            CreateMap<DiscussionReply, DiscussionReplyResponse>();
        }
    }
}