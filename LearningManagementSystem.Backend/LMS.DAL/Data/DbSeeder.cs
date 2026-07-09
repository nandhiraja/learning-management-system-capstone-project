using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LMS.Core.Models;
using LMS.Core.Enums;

namespace LMS.DAL.Data
{
    public static class DbSeeder
    {
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static async Task ClearDatabaseAsync(LMSDBContext context)
        {
            // Delete dependent records first to satisfy foreign keys
            context.DiscussionReplyLikes.RemoveRange(context.DiscussionReplyLikes);
            context.DiscussionReplies.RemoveRange(context.DiscussionReplies);
            context.Discussions.RemoveRange(context.Discussions);
            context.Payments.RemoveRange(context.Payments);
            context.OrderItems.RemoveRange(context.OrderItems);
            context.Orders.RemoveRange(context.Orders);
            context.Certificates.RemoveRange(context.Certificates);
            context.Enrollments.RemoveRange(context.Enrollments);
            context.CourseReviews.RemoveRange(context.CourseReviews);
            context.LectureProgresses.RemoveRange(context.LectureProgresses);
            context.QuizOptions.RemoveRange(context.QuizOptions);
            context.QuizQuestions.RemoveRange(context.QuizQuestions);
            context.Quizzes.RemoveRange(context.Quizzes);
            context.Lectures.RemoveRange(context.Lectures);
            context.CourseSections.RemoveRange(context.CourseSections);
            context.Courses.RemoveRange(context.Courses);
            context.Languages.RemoveRange(context.Languages);
            context.Categories.RemoveRange(context.Categories);
            context.UserRefreshTokens.RemoveRange(context.UserRefreshTokens);
            context.Users.RemoveRange(context.Users);
            context.Roles.RemoveRange(context.Roles);

            await context.SaveChangesAsync();
        }

        public static async Task SeedDatabaseAsync(LMSDBContext context)
        {
            await ClearDatabaseAsync(context);

            // 1. Roles
            var adminRole = new Role { Id = 1, Name = "Admin" };
            var instructorRole = new Role { Id = 2, Name = "Instructor" };
            var studentRole = new Role { Id = 3, Name = "Student" };

            context.Roles.AddRange(adminRole, instructorRole, studentRole);
            await context.SaveChangesAsync();

            // 2. Users
            var adminUser = new User
            {
                UserName = "admin",
                FirstName = "LMS",
                LastName = "Administrator",
                Email = "admin@lms.com",
                PasswordHash = HashPassword("Password123"),
                PhoneNo = "1234567890",
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var instructor1 = new User
            {
                UserName = "johndoe",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@lms.com",
                PasswordHash = HashPassword("Password123"),
                PhoneNo = "9876543210",
                RoleId = instructorRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var instructor2 = new User
            {
                UserName = "janesmith",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@lms.com",
                PasswordHash = HashPassword("Password123"),
                PhoneNo = "5551234567",
                RoleId = instructorRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var studentActive = new User
            {
                UserName = "studentactive",
                FirstName = "Active",
                LastName = "Student",
                Email = "student.active@lms.com",
                PasswordHash = HashPassword("Password123"),
                PhoneNo = "1112223333",
                RoleId = studentRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var studentNew = new User
            {
                UserName = "studentnew",
                FirstName = "New",
                LastName = "Student",
                Email = "student.new@lms.com",
                PasswordHash = HashPassword("Password123"),
                PhoneNo = "4445556666",
                RoleId = studentRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(adminUser, instructor1, instructor2, studentActive, studentNew);
            await context.SaveChangesAsync();

            // 3. Languages & Categories
            var langEn = new Language { Name = "English", IsApproved = true };
            var langEs = new Language { Name = "Spanish", IsApproved = true };
            var langFr = new Language { Name = "French", IsApproved = true };
            context.Languages.AddRange(langEn, langEs, langFr);

            var catDev = new Category { Name = "Software Development", IsApproved = true };
            var catDesign = new Category { Name = "Design", IsApproved = true };
            var catBusiness = new Category { Name = "Business", IsApproved = true };
            context.Categories.AddRange(catDev, catDesign, catBusiness);

            await context.SaveChangesAsync();

            // 4. Courses
            var course1 = new Course
            {
                InstructorId = instructor1.Id,
                CategoryId = catDev.Id,
                LanguageId = langEn.Id,
                Title = "Full-Stack Web Development with ASP.NET Core & Angular",
                Description = "This comprehensive course covers Backend Web API design using ASP.NET Core, Npgsql, EF Core, and Frontend implementation using Angular, RxJS, and TypeScript.",
                Price = 99.99M,
                ThumbnailUrl = "/files/thumbnails/dotnet-angular.png",
                Status = CourseStatus.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var course2 = new Course
            {
                InstructorId = instructor1.Id,
                CategoryId = catDev.Id,
                LanguageId = langEn.Id,
                Title = "Introduction to Python and Data Science",
                Description = "Get started with Python, NumPy, Pandas, and Matplotlib. Perfect for beginners entering the data analytics space.",
                Price = 49.99M,
                ThumbnailUrl = "/files/thumbnails/python-ds.png",
                Status = CourseStatus.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var course3 = new Course
            {
                InstructorId = instructor2.Id,
                CategoryId = catDesign.Id,
                LanguageId = langEn.Id,
                Title = "Advanced UX/UI Design Patterns",
                Description = "Deep dive into typography, color systems, component libraries, and interactive prototype animations in Figma.",
                Price = 79.99M,
                ThumbnailUrl = "/files/thumbnails/uxui-design.png",
                Status = CourseStatus.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Courses.AddRange(course1, course2, course3);
            await context.SaveChangesAsync();

            // 5. Course Sections
            var sec1 = new CourseSection
            {
                CourseId = course1.Id,
                Title = "Getting Started & Project Setup",
                Description = "Introduction, environment setup, and basic boilerplate structures.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var sec2 = new CourseSection
            {
                CourseId = course1.Id,
                Title = "Building the Backend REST API",
                Description = "Database integration, repositories, services, and controller exposure.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var sec3 = new CourseSection
            {
                CourseId = course2.Id,
                Title = "Python Basics",
                Description = "Core features of python language including control flow and basic structures.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.CourseSections.AddRange(sec1, sec2, sec3);
            await context.SaveChangesAsync();

            // 6. Lectures
            var lec1 = new Lecture
            {
                CourseSectionId = sec1.Id,
                Title = "Introduction to the Course",
                ContentUrl = "/files/lectures/intro.mp4",
                ContentType = ContentType.Video,
                DurationInMinutes = 10,
                Status = LectureStatus.NotStarted
            };

            var lec2 = new Lecture
            {
                CourseSectionId = sec1.Id,
                Title = "Angular Application Architecture Setup",
                ContentUrl = "/files/lectures/angular-setup.mp4",
                ContentType = ContentType.Video,
                DurationInMinutes = 20,
                Status = LectureStatus.NotStarted
            };

            var lec3 = new Lecture
            {
                CourseSectionId = sec2.Id,
                Title = "Designing the Database Schema",
                ContentUrl = "https://example.com/db-schema-guide",
                ContentType = ContentType.ExternalLink,
                DurationInMinutes = 15,
                Status = LectureStatus.NotStarted
            };

            var lec4 = new Lecture
            {
                CourseSectionId = sec3.Id,
                Title = "Variables, Types, and Control Flow",
                ContentUrl = "/files/lectures/python-basics.mp4",
                ContentType = ContentType.Video,
                DurationInMinutes = 25,
                Status = LectureStatus.NotStarted
            };

            context.Lectures.AddRange(lec1, lec2, lec3, lec4);
            await context.SaveChangesAsync();

            // 7. Quizzes
            var quiz1 = new Quiz
            {
                CourseId = course1.Id,
                LectureId = lec2.Id,
                Title = "Angular Core Concepts Quiz",
                Description = "Test your understanding of Angular modules, components, and services.",
                TotalMarks = 10,
                PassScore = 6,
                MaxAttempts = 3,
                Status = QuizStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Quizzes.Add(quiz1);
            await context.SaveChangesAsync();

            // 8. Quiz Questions & Options
            var q1 = new QuizQuestion { QuizId = quiz1.Id, QuestionText = "Which decorator is used to define an Angular component?" };
            context.QuizQuestions.Add(q1);
            await context.SaveChangesAsync();

            var opt1 = new QuizOption { QuizQuestionId = q1.Id, OptionText = "@Injectable", IsCorrect = false };
            var opt2 = new QuizOption { QuizQuestionId = q1.Id, OptionText = "@Directive", IsCorrect = false };
            var opt3 = new QuizOption { QuizQuestionId = q1.Id, OptionText = "@Component", IsCorrect = true };
            var opt4 = new QuizOption { QuizQuestionId = q1.Id, OptionText = "@NgModule", IsCorrect = false };
            context.QuizOptions.AddRange(opt1, opt2, opt3, opt4);
            await context.SaveChangesAsync();

            // 9. Orders, OrderItems, Payments, Enrollments, and Reviews
            var order = new Order
            {
                UserId = studentActive.Id,
                Amount = course1.Price,
                Status = OrderStatus.Completed,
                OrderDate = DateTime.UtcNow
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                CourseId = course1.Id,
                Price = course1.Price
            };
            context.OrderItems.Add(orderItem);
            await context.SaveChangesAsync();

            var enrollment = new Enrollment
            {
                UserId = studentActive.Id,
                CourseId = course1.Id,
                OrderItemId = orderItem.Id,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow
            };
            context.Enrollments.Add(enrollment);

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = course1.Price,
                PaymentMethod = PaymentMethod.CreditCard,
                Status = PaymentStatus.Success,
                TransactionId = "TXN_" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                PaymentDate = DateTime.UtcNow
            };
            context.Payments.Add(payment);

            var review = new CourseReview
            {
                UserId = studentActive.Id,
                CourseId = course1.Id,
                Rating = 5,
                Comment = "Excellent course! Highly structured and clean code examples.",
                CreatedAt = DateTime.UtcNow
            };
            context.CourseReviews.Add(review);

            // 10. Discussions
            var discussion = new Discussion
            {
                CourseId = course1.Id,
                UserId = studentActive.Id,
                Title = "Angular 19 compatibility question",
                Content = "Does this project work with the latest standalone component features in Angular 19?",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Discussions.Add(discussion);
            await context.SaveChangesAsync();

            var reply = new DiscussionReply
            {
                DiscussionId = discussion.Id,
                UserId = instructor1.Id,
                Content = "Yes! All frontend resources are updated to Angular 19 and use standalone components by default.",
                CreatedAt = DateTime.UtcNow
            };
            context.DiscussionReplies.Add(reply);
            await context.SaveChangesAsync();

            // Reset sequences in PostgreSQL for serial ID columns so that future database writes don't encounter duplicate key errors
            var tables = new[] { "Roles", "Users", "Languages", "Categories", "Courses", "CourseSections", "Lectures", "Quizzes", "QuizQuestions", "QuizOptions", "Enrollments", "Orders", "OrderItems", "Payments", "CourseReviews", "Discussions", "DiscussionReplies" };
            foreach (var table in tables)
            {
                try{
                    #pragma warning disable EF1002
                    await context.Database.ExecuteSqlRawAsync($"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), COALESCE(MAX(\"Id\"), 1)) FROM \"{table}\";");
                    #pragma warning restore EF1002
                }
                catch
                {
                    // Ignore sequence resets for tables that might not have custom sequences or are already updated
                }
            }
        }
    }
}
