using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(cat => cat.Id);

            builder.Property(cat => cat.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(cat => cat.Name)
                .IsUnique();

            builder.Property(cat => cat.CreatedAt)
                .IsRequired();

            builder.Property(cat => cat.IsApproved)
                .IsRequired()
                .HasDefaultValue(false);
        }
    }
}
