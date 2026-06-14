using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class LanguageConfiguration : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(l => l.Name)
                .IsUnique();

            builder.Property(l => l.IsApproved)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(l => l.CreatedAt)
                .IsRequired();
        }
    }
}
