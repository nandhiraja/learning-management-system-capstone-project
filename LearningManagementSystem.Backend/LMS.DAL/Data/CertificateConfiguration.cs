using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
    {
        public void Configure(EntityTypeBuilder<Certificate> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CertificateUrl)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(c => c.IssuedDate)
                .IsRequired();

            // Configure unique index on EnrollmentId to enforce one-to-one relationship
            builder.HasIndex(c => c.EnrollmentId)
                .IsUnique();

            // Configure relationships
            builder.HasOne(c => c.User)
                .WithMany(u => u.Certificates)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Enrollment)
                .WithOne(e => e.Certificate)
                .HasForeignKey<Certificate>(c => c.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
