using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
    {
        public void Configure(EntityTypeBuilder<QuizQuestion> builder)
        {
            builder.HasKey(qq => qq.Id);

            builder.Property(qq => qq.QuestionText)
                .IsRequired()
                .HasMaxLength(1000);

            // Configure relationships
            builder.HasOne(qq => qq.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
