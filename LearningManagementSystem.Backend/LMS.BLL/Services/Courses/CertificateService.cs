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
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IMapper _mapper;

        public CertificateService(
            ICertificateRepository certificateRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IMapper mapper)
        {
            _certificateRepository = certificateRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _mapper = mapper;
        }

        public async Task<CertificateResponse?> GetCertificateAsync(Guid courseGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            var certificates = await _certificateRepository.GetCertificatesByUserIdAsync(user.Id);
            var certificate = certificates.FirstOrDefault(c => c.CourseId == course.Id);
            if (certificate == null) return null;

            return _mapper.Map<CertificateResponse>(certificate);
        }

        public async Task<CertificateResponse> GenerateCertificateAsync(Guid courseGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == course.Id);
            if (enrollment == null)
                throw new InvalidOperationException("User is not enrolled in this course and cannot be issued a certificate.");

            var certificates = await _certificateRepository.GetCertificatesByUserIdAsync(user.Id);
            var existingCert = certificates.FirstOrDefault(c => c.CourseId == course.Id);
            if (existingCert != null)
            {
                return _mapper.Map<CertificateResponse>(existingCert);
            }

            var certificate = new Certificate
            {
                UserId = user.Id,
                CourseId = course.Id,
                EnrollmentId = enrollment.Id,
                IssuedDate = DateTime.UtcNow,
                CertificateUrl = $"/certificates/verify/{Guid.NewGuid()}"
            };

            var createdCertificate = await _certificateRepository.Create(certificate);
            return _mapper.Map<CertificateResponse>(createdCertificate);
        }

        public async Task<CertificateResponse?> GetCertificateByIdAsync(int certificateId)
        {
            var certificate = await _certificateRepository.Get(certificateId);
            if (certificate == null) return null;
            return _mapper.Map<CertificateResponse>(certificate);
        }
    }
}

