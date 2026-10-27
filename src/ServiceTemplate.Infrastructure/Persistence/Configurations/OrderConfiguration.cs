using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTemplate.Domain.Orders;

namespace ServiceTemplate.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt);

        builder.OwnsOne(o => o.TotalAmount, m =>
        {
            m.Property(p => p.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 4)
                .IsRequired();

            m.Property(p => p.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Street)
                .HasColumnName("shipping_street")
                .HasMaxLength(200)
                .IsRequired();

            a.Property(p => p.City)
                .HasColumnName("shipping_city")
                .HasMaxLength(100)
                .IsRequired();

            a.Property(p => p.Country)
                .HasColumnName("shipping_country")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(o => o.Version)
            .IsRowVersion();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
