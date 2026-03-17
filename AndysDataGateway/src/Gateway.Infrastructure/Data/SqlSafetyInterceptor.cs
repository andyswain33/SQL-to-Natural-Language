using System.Text.RegularExpressions;

namespace Gateway.Infrastructure.Data
{
    /// <summary>
    /// Acts as the final checkpoint before executing AI-generated SQL against the database.
    /// </summary>
    public class SqlSafetyInterceptor
    {
        // A strict blacklist of DDL and DML commands.
        private static readonly string[] ForbiddenKeywords =
        {
            "UPDATE", "DELETE", "INSERT", "DROP", "ALTER", "CREATE",
            "EXEC", "EXECUTE", "TRUNCATE", "MERGE", "GRANT", "REVOKE"
        };

        /// <summary>
        /// Validates the AI-generated SQL against enterprise safety rules.
        /// </summary>
        public bool IsQuerySafe(string generatedSql, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(generatedSql))
            {
                errorMessage = "The AI generated an empty query.";
                return false;
            }

            var upperSql = generatedSql.Trim().ToUpperInvariant();

            // 1. Must be a Read-Only operation
            if (!upperSql.StartsWith("SELECT") && !upperSql.StartsWith("WITH"))
            {
                errorMessage = "Security Violation: Query must be a SELECT statement or a CTE (WITH).";
                return false;
            }

            // 2. Scan for Destructive Keywords using Word Boundaries (\b)
            // This prevents false positives (e.g., blocking a column named 'DropOffTime' because it contains 'DROP')
            foreach (var keyword in ForbiddenKeywords)
            {
                var pattern = $@"\b{keyword}\b";
                if (Regex.IsMatch(upperSql, pattern))
                {
                    errorMessage = $"Security Violation: Query contains forbidden operational keyword '{keyword}'.";
                    return false;
                }
            }

            // 3. Enforce View-Only Access 
            // Ensures the query targets our abstraction layer (vw_) and not base tables.
            if (!upperSql.Contains("VW_"))
            {
                errorMessage = "Security Violation: Query does not target an authorized read-only view.";
                return false;
            }

            // If it passes all checks, it is cleared for execution.
            return true;
        }
    }
}