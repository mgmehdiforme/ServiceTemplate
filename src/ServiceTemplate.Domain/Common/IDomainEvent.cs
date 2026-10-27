namespace ServiceTemplate.Domain.Common;

/// <summary>
/// Marker interface for all domain events indicating a significant business state change.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
