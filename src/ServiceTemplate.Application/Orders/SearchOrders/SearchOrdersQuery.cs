namespace ServiceTemplate.Application.Orders.SearchOrders;

/// <summary>
/// Natural language query for semantically searching orders using pgvector and AI synthesis.
/// </summary>
public sealed record SearchOrdersQuery(string Query, int TopK = 5);
