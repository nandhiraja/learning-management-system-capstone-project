using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
    {
        public void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Status)
                .IsRequired();

            builder.Property(e => e.EnrolledAt)
                .IsRequired();

            // Unique index to prevent duplicate enrollments for same course/user
            builder.HasIndex(e => new { e.CourseId, e.UserId })
                .IsUnique();

            // Configure relationships
            builder.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.OrderItem)
                .WithMany() // OrderItem doesn't need list of enrollments, but it maps back
                .HasForeignKey(e => e.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
