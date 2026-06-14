using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class DiscussionReplyConfiguration : IEntityTypeConfiguration<DiscussionReply>
    {
        public void Configure(EntityTypeBuilder<DiscussionReply> builder)
        {
            builder.HasKey(dr => dr.Id);

            builder.Property(dr => dr.ExternalId)
                .IsRequired();

            builder.Property(dr => dr.Content)
                .IsRequired()
                .HasMaxLength(5000);

            builder.Property(dr => dr.CreatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(dr => dr.Discussion)
                .WithMany(d => d.Replies)
                .HasForeignKey(dr => dr.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dr => dr.User)
                .WithMany()
                .HasForeignKey(dr => dr.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
