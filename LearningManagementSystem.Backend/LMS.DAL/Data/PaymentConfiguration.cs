using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(p => p.PaymentMethod)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.TransactionId)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.PaymentDate)
                .IsRequired();

            // Configure unique index on OrderId to enforce one-to-one relationship
            builder.HasIndex(p => p.OrderId)
                .IsUnique();

            // Configure relationships
            builder.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
