using System.Text;

namespace Gateway.Core.Mapping;

public class MetadataMapper
{
    private readonly Dictionary<string, AuthorizedSchemaObject> _semanticMap;

    public MetadataMapper()
    {
        // The "White-list" mapping business domains to read-only views
        _semanticMap = new Dictionary<string, AuthorizedSchemaObject>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Identities",
                new AuthorizedSchemaObject
                {
                    ObjectName = "vw_ActiveIdentities",
                    Description = "Use this to query information about active personnel, their departments, and clearance levels.",
                    AllowedColumns = ["IdentityID", "FirstName", "LastName", "Email", "Department", "JobTitle", "ClearanceLevel"]
                }
            },
            {
                "SecurityLogs",
                new AuthorizedSchemaObject
                {
                    ObjectName = "vw_HighSecurityAccessLogs",
                    Description = "Use this to audit access events in high-security zones. Includes granted/denied status, denial reasons, and anomaly risk scores.",
                    AllowedColumns = ["AttemptTimestamp", "ZoneName", "Building", "IdentityName", "CredentialType", "IsGranted", "DenialReason", "RiskScore"]
                }
            }
        };
    }

    /// <summary>
    /// Core Intent Engine: Determines which schema objects are relevant based on the user's prompt.
    /// </summary>
    public IEnumerable<AuthorizedSchemaObject> GetRelevantSchemaObjects(string userPrompt)
    {
        var relevantViews = new HashSet<AuthorizedSchemaObject>();
        var promptLower = userPrompt.ToLowerInvariant();

        // 1. Basic Intent Matching
        if (promptLower.Contains("user") || promptLower.Contains("employee") || promptLower.Contains("clearance"))
        {
            relevantViews.Add(_semanticMap["Identities"]);
        }
        if (promptLower.Contains("access") || promptLower.Contains("deny") || promptLower.Contains("security") || promptLower.Contains("log") || promptLower.Contains("risk"))
        {
            relevantViews.Add(_semanticMap["SecurityLogs"]);
        }

        // 2. Fallback: If intent is ambiguous, inject all safe views.
        if (!relevantViews.Any())
        {
            foreach (var view in _semanticMap.Values) relevantViews.Add(view);
        }

        return relevantViews;
    }

    /// <summary>
    /// Analyzes the user prompt and returns only the necessary schema context for the LLM.
    /// </summary>
    public string GetSchemaContext(string userPrompt)
    {
        var relevantViews = GetRelevantSchemaObjects(userPrompt);

        // 3. Format strictly for LLM Consumption
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("CRITICAL SYSTEM INSTRUCTION: You are a secure Text-to-SQL engine.");
        contextBuilder.AppendLine("You may ONLY query the following read-only objects and columns. Do not invent tables.");
        contextBuilder.AppendLine("=========================================");

        foreach (var view in relevantViews)
        {
            contextBuilder.AppendLine($"- Target: {view.ObjectName}");
            contextBuilder.AppendLine($"  Description: {view.Description}");
            contextBuilder.AppendLine($"  Available Columns: {string.Join(", ", view.AllowedColumns)}");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    /// <summary>
    /// Returns the names of the views authorized for a given prompt.
    /// To be consumed by the AST Security Visitor for strict validation.
    /// </summary>
    public IEnumerable<string> GetAuthorizedViewNames(string userPrompt)
    {
        return GetRelevantSchemaObjects(userPrompt).Select(v => v.ObjectName);
    }
}