namespace Gateway.Core.Mapping;

/// <summary>
/// Represents a read-only view authorized for LLM querying.
/// </summary>
public class AuthorizedSchemaObject
{
    public required string ObjectName { get; set; }
    public required string Description { get; set; }
    public required List<string> AllowedColumns { get; set; }
}