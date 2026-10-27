namespace ServiceTemplate.Contracts.Common;

/// <summary>
/// Base record for all public asynchronous integration events.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
