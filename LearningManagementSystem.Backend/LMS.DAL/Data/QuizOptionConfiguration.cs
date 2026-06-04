using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
    {
        public void Configure(EntityTypeBuilder<QuizOption> builder)
        {
            builder.HasKey(qo => qo.Id);

            builder.Property(qo => qo.OptionText)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(qo => qo.IsCorrect)
                .IsRequired();

            // Configure relationships
            builder.HasOne(qo => qo.QuizQuestion)
                .WithMany(qq => qq.Options)
                .HasForeignKey(qo => qo.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
