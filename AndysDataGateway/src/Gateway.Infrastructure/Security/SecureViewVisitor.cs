using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Gateway.Infrastructure.Security;

/// <summary>
/// Traverses the SQL AST to ensure all accessed tables/views are explicitly authorized.
/// </summary>
public class SecureViewVisitor : TSqlFragmentVisitor
{
    public List<string> SecurityViolations { get; } = new();
    private readonly HashSet<string> _authorizedViews;

    public SecureViewVisitor(IEnumerable<string> authorizedViews)
    {
        // Enforce case-insensitive comparison for SQL object names
        _authorizedViews = new HashSet<string>(authorizedViews, StringComparer.OrdinalIgnoreCase);
    }

    // This method is automatically invoked for every table reference in the query,
    // including those hidden deep within subqueries or JOIN clauses.
    public override void ExplicitVisit(NamedTableReference node)
    {
        // Extract the base object name (e.g., "vw_ActiveIdentities")
        var objectName = node.SchemaObject.BaseIdentifier.Value;

        if (!_authorizedViews.Contains(objectName))
        {
            SecurityViolations.Add($"Security Violation: Unauthorized access to underlying object '{objectName}' is strictly prohibited.");
        }

        // Continue traversing down the tree
        base.ExplicitVisit(node);
    }
}