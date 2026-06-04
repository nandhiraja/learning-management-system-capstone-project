using LMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.DAL.Data
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.Id);

            builder.Property(oi => oi.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(oi => oi.DiscountPercentage)
                .HasPrecision(5, 2) // Typically percentage has smaller scale but 18,2 is fine. Let's use 18,2 to match type.
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(oi => oi.FinalPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            // Configure relationships
            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(oi => oi.Course)
                .WithMany(c => c.OrderItems)
                .HasForeignKey(oi => oi.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
