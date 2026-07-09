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
        private readonly ICertificatePdfGenerator _pdfGenerator;
        private readonly IFileStorageService _fileStorageService;
        private readonly IRealTimeNotificationService _realTimeNotificationService;

        public CertificateService(
            ICertificateRepository certificateRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            ICertificatePdfGenerator pdfGenerator,
            IFileStorageService fileStorageService,
            IRealTimeNotificationService realTimeNotificationService,
            IMapper mapper)
        {
            _certificateRepository = certificateRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _pdfGenerator = pdfGenerator;
            _fileStorageService = fileStorageService;
            _realTimeNotificationService = realTimeNotificationService;
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

            bool isUpdated = false;
            if (string.IsNullOrEmpty(certificate.VerificationId))
            {
                certificate.VerificationId = Guid.NewGuid().ToString("N").ToUpper();
                isUpdated = true;
            }

            if (string.IsNullOrEmpty(certificate.CertificateUrl) || !certificate.CertificateUrl.EndsWith(".pdf") || isUpdated)
            {
                certificate.CertificateUrl = await GenerateAndSavePdfAsync(user, course, certificate);
                isUpdated = true;
            }

            if (isUpdated)
            {
                await _certificateRepository.Update(certificate);
            }

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
                bool isUpdated = false;
                if (string.IsNullOrEmpty(existingCert.VerificationId))
                {
                    existingCert.VerificationId = Guid.NewGuid().ToString("N").ToUpper();
                    isUpdated = true;
                }
                if (string.IsNullOrEmpty(existingCert.CertificateUrl) || !existingCert.CertificateUrl.EndsWith(".pdf") || isUpdated)
                {
                    existingCert.CertificateUrl = await GenerateAndSavePdfAsync(user, course, existingCert);
                    isUpdated = true;
                }
                if (isUpdated)
                {
                    await _certificateRepository.Update(existingCert);
                }
                return _mapper.Map<CertificateResponse>(existingCert);
            }

            var studentName = !string.IsNullOrEmpty(user.CertificateName) ? user.CertificateName : $"{user.FirstName} {user.LastName}".Trim();

            var certificate = new Certificate
            {
                UserId = user.Id,
                CourseId = course.Id,
                EnrollmentId = enrollment.Id,
                RecipientFullName = studentName,
                IssuedDate = DateTime.UtcNow,
                VerificationId = Guid.NewGuid().ToString("N").ToUpper(),
                CertificateUrl = "" 
            };

            certificate.CertificateUrl = await GenerateAndSavePdfAsync(user, course, certificate);

            var createdCertificate = await _certificateRepository.Create(certificate);

            try
            {
                await _realTimeNotificationService.CreateAndSendNotificationAsync(user.Id, "Certificate Earned", $"Congratulations! You have successfully earned your certificate for: {course.Title}.", "Certificate");
            }
            catch (Exception)
            {
            }

            return _mapper.Map<CertificateResponse>(createdCertificate);
        }

        private async Task<string> GenerateAndSavePdfAsync(User user, Course course, Certificate certificate)
        {
            var instructorName = "Instructor";
            if (course.InstructorId != 0) {
                var instructor = await _userRepository.GetUserWithRoleAsync(course.InstructorId);
                if (instructor != null) {
                    instructorName = $"{instructor.FirstName} {instructor.LastName}".Trim();
                }
            }

            if (string.IsNullOrEmpty(certificate.VerificationId))
            {
                certificate.VerificationId = Guid.NewGuid().ToString("N").ToUpper();
            }
            var certificateIdStr = certificate.VerificationId;
            var studentName = !string.IsNullOrEmpty(certificate.RecipientFullName) ? certificate.RecipientFullName : $"{user.FirstName} {user.LastName}".Trim();

            var pdfBytes = _pdfGenerator.GenerateCertificatePdf(studentName, course.Title, instructorName, certificate.IssuedDate.ToString("MMMM dd, yyyy"), certificateIdStr, user.CertificateNameChangesCount);
            return await _fileStorageService.SaveFileAsync(pdfBytes, $"{certificateIdStr}.pdf", "certificates");
        }

        public async Task<CertificateResponse?> GetCertificateByIdAsync(int certificateId)
        {
            var certificate = await _certificateRepository.Get(certificateId);
            if (certificate == null) return null;

            bool isUpdated = false;
            if (string.IsNullOrEmpty(certificate.VerificationId))
            {
                certificate.VerificationId = Guid.NewGuid().ToString("N").ToUpper();
                isUpdated = true;
            }

            if (string.IsNullOrEmpty(certificate.CertificateUrl) || !certificate.CertificateUrl.EndsWith(".pdf") || isUpdated)
            {
                var user = await _userRepository.GetUserWithRoleAsync(certificate.UserId);
                var course = await _courseRepository.Get(certificate.CourseId);
                if (user != null && course != null)
                {
                    certificate.CertificateUrl = await GenerateAndSavePdfAsync(user, course, certificate);
                    isUpdated = true;
                }
            }

            if (isUpdated)
            {
                await _certificateRepository.Update(certificate);
            }

            return _mapper.Map<CertificateResponse>(certificate);
        }

        public async Task<IEnumerable<CertificateResponse>> GetCertificatesByUserAsync(Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var certificates = await _certificateRepository.GetCertificatesByUserIdAsync(user.Id);
            
            foreach (var cert in certificates)
            {
                bool isUpdated = false;
                if (string.IsNullOrEmpty(cert.VerificationId))
                {
                    cert.VerificationId = Guid.NewGuid().ToString("N").ToUpper();
                    isUpdated = true;
                }

                if (string.IsNullOrEmpty(cert.CertificateUrl) || !cert.CertificateUrl.EndsWith(".pdf") || isUpdated)
                {
                    var course = await _courseRepository.Get(cert.CourseId);
                    if (course != null)
                    {
                        cert.CertificateUrl = await GenerateAndSavePdfAsync(user, course, cert);
                        isUpdated = true;
                    }
                }

                if (isUpdated)
                {
                    await _certificateRepository.Update(cert);
                }
            }

            return _mapper.Map<IEnumerable<CertificateResponse>>(certificates);
        }

        public async Task<int> RegenerateAllCertificatesAsync()
        {
            var certificates = await _certificateRepository.GetAllAsync();
            int count = 0;

            foreach (var cert in certificates)
            {
                var user = await _userRepository.GetUserWithRoleAsync(cert.UserId);
                var course = await _courseRepository.Get(cert.CourseId);

                if (user != null && course != null)
                {
                    if (string.IsNullOrEmpty(cert.VerificationId))
                    {
                        cert.VerificationId = Guid.NewGuid().ToString("N").ToUpper();
                    }
                    cert.CertificateUrl = await GenerateAndSavePdfAsync(user, course, cert);
                    await _certificateRepository.Update(cert);
                    count++;
                }
            }

            return count;
        }

        public async Task<CertificateResponse?> GetCertificateByVerificationIdAsync(string verificationId)
        {
            var certificate = await _certificateRepository.GetByVerificationIdAsync(verificationId);
            if (certificate == null) return null;

            bool isUpdated = false;
            if (string.IsNullOrEmpty(certificate.CertificateUrl) || !certificate.CertificateUrl.EndsWith(".pdf"))
            {
                var user = await _userRepository.GetUserWithRoleAsync(certificate.UserId);
                var course = await _courseRepository.Get(certificate.CourseId);
                if (user != null && course != null)
                {
                    certificate.CertificateUrl = await GenerateAndSavePdfAsync(user, course, certificate);
                    isUpdated = true;
                }
            }

            if (isUpdated)
            {
                await _certificateRepository.Update(certificate);
            }

            return _mapper.Map<CertificateResponse>(certificate);
        }
    }
}

