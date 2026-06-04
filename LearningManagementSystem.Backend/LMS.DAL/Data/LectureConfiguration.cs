using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class LectureConfiguration : IEntityTypeConfiguration<Lecture>
    {
        public void Configure(EntityTypeBuilder<Lecture> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.ContentUrl)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(l => l.ContentType)
                .IsRequired();

            builder.Property(l => l.DurationInMinutes)
                .IsRequired();

            builder.Property(l => l.Status)
                .IsRequired();

            // Configure relationships
            builder.HasOne(l => l.CourseSection)
                .WithMany(cs => cs.Lectures)
                .HasForeignKey(l => l.CourseSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
