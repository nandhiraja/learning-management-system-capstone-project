using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class LectureProgressConfiguration : IEntityTypeConfiguration<LectureProgress>
    {
        public void Configure(EntityTypeBuilder<LectureProgress> builder)
        {
            builder.HasKey(lp => lp.Id);

            builder.Property(lp => lp.Status)
                .IsRequired();

            // Configure unique index to prevent duplicate progress entries per lecture/enrollment
            builder.HasIndex(lp => new { lp.EnrollmentId, lp.LectureId })
                .IsUnique();

            // Configure relationships
            builder.HasOne(lp => lp.Lecture)
                .WithMany(l => l.LectureProgresses)
                .HasForeignKey(lp => lp.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(lp => lp.Enrollment)
                .WithMany(e => e.LectureProgresses)
                .HasForeignKey(lp => lp.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
