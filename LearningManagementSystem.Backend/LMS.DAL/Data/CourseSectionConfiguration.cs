using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class CourseSectionConfiguration : IEntityTypeConfiguration<CourseSection>
    {
        public void Configure(EntityTypeBuilder<CourseSection> builder)
        {
            builder.HasKey(cs => cs.Id);

            builder.Property(cs => cs.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(cs => cs.Description)
                .HasMaxLength(2000);

            builder.Property(cs => cs.Order)
                .IsRequired();

            builder.Property(cs => cs.CreatedAt)
                .IsRequired();

            builder.Property(cs => cs.UpdatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(cs => cs.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
