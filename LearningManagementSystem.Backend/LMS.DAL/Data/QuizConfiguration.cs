using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.HasKey(q => q.Id);

            builder.Property(q => q.QuizName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(q => q.Description)
                .HasMaxLength(2000);

            builder.Property(q => q.TotalMarks)
                .IsRequired();

            builder.Property(q => q.PassingMarks)
                .IsRequired();

            builder.Property(q => q.MaxAttempts)
                .IsRequired();

            builder.Property(q => q.CurrentAttempt)
                .IsRequired();

            builder.Property(q => q.Status)
                .IsRequired();

            builder.Property(q => q.CreatedAt)
                .IsRequired();

            builder.Property(q => q.UpdatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(q => q.Course)
                .WithMany() // Courses don't have direct list of quizzes, they have sections and lectures
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(q => q.Lecture)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
