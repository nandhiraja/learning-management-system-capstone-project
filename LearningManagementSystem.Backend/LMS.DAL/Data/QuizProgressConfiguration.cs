using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class QuizProgressConfiguration : IEntityTypeConfiguration<QuizProgress>
    {
        public void Configure(EntityTypeBuilder<QuizProgress> builder)
        {
            builder.HasKey(qp => new { qp.UserId, qp.QuizId });

            builder.HasOne(qp => qp.User)
                .WithMany(u => u.QuizProgresses)
                .HasForeignKey(qp => qp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(qp => qp.Quiz)
                .WithMany() // Quiz doesn't strictly need a collection of progress objects unless needed
                .HasForeignKey(qp => qp.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
