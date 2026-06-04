using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(2000);

            builder.Property(c => c.ThumbnailUrl)
                .HasMaxLength(2000);

            builder.Property(c => c.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(c => c.DiscountPercentage)
                .IsRequired();

            builder.Property(c => c.DurationInMinutes)
                .IsRequired();

            builder.Property(c => c.Status)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(c => c.Instructor)
                .WithMany(u => u.CreatedCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Category)
                .WithMany(cat => cat.Courses)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Language)
                .WithMany(l => l.Courses)
                .HasForeignKey(c => c.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
