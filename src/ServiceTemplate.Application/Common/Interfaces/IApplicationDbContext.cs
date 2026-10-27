using Microsoft.EntityFrameworkCore;
using ServiceTemplate.Domain.Orders;

namespace ServiceTemplate.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the primary application database context.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
