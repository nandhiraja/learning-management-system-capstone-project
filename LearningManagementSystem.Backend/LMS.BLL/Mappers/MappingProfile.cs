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
            CreateMap<User, UserProfileResponse>();
            CreateMap<RegisterRequest, User>();
            CreateMap<UserEditRequest, User>();

            // Course mappings
            CreateMap<Course, CourseResponse>();
            CreateMap<CourseCreateRequest, Course>();
            CreateMap<CourseUpdateRequest, Course>();

            // Section mappings
            CreateMap<CourseSection, CourseSectionResponse>();
            CreateMap<CourseSectionRequest, CourseSection>();

            // Lecture mappings
            CreateMap<Lecture, LectureResponse>();
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

            // Enrollment mappings (Leverages AutoMapper member flattening for Course.ExternalId -> CourseExternalId and Course.Title -> CourseTitle)
            CreateMap<Enrollment, EnrollmentResponse>();

            // Cart mappings
            CreateMap<Course, CartItemResponse>();

            // Progress mappings
            CreateMap<ProgressUpdateRequest, LectureProgress>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsCompleted ? LectureStatus.Completed : LectureStatus.InProgress));

            // Review mappings 
            CreateMap<CourseReview, ReviewResponse>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty));
            CreateMap<ReviewRequest, CourseReview>();

            // Certificate mappings
            CreateMap<Certificate, CertificateResponse>();
        }
    }
}