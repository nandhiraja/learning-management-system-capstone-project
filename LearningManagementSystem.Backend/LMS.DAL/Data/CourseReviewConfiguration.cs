using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
    {
        public void Configure(EntityTypeBuilder<CourseReview> builder)
        {
            builder.HasKey(cr => cr.Id);

            builder.Property(cr => cr.Rating)
                .IsRequired();

            builder.Property(cr => cr.Comment)
                .HasMaxLength(2000);

            builder.Property(cr => cr.CreatedAt)
                .IsRequired();

            // Configure unique index so a user can review a course only once
            builder.HasIndex(cr => new { cr.CourseId, cr.UserId })
                .IsUnique();

            // Configure relationships
            builder.HasOne(cr => cr.Course)
                .WithMany(c => c.CourseReviews)
                .HasForeignKey(cr => cr.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cr => cr.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
