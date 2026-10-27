using Microsoft.EntityFrameworkCore;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Orders;

namespace ServiceTemplate.UnitTests.Common;

public class TestApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
        : base(options)
    {
    }

    public static TestApplicationDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.OwnsOne(o => o.TotalAmount, m =>
            {
                m.Property(p => p.Amount).HasPrecision(18, 4);
                m.Property(p => p.Currency).HasMaxLength(3);
            });
            builder.OwnsOne(o => o.ShippingAddress, a =>
            {
                a.Property(p => p.Street).HasMaxLength(200);
                a.Property(p => p.City).HasMaxLength(100);
                a.Property(p => p.Country).HasMaxLength(100);
            });
            builder.HasMany(o => o.Items).WithOne().HasForeignKey("order_id");
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.OwnsOne(i => i.UnitPrice, m =>
            {
                m.Property(p => p.Amount).HasPrecision(18, 4);
                m.Property(p => p.Currency).HasMaxLength(3);
            });
        });
    }
}
