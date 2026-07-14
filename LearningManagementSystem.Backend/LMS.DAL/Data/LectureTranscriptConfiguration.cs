using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class LectureTranscriptConfiguration : IEntityTypeConfiguration<LectureTranscript>
    {
        public void Configure(EntityTypeBuilder<LectureTranscript> builder)
        {
            builder.HasKey(t => t.Id);

            builder.HasOne(t => t.Lecture)
                .WithMany(l => l.Transcripts)
                .HasForeignKey(t => t.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
