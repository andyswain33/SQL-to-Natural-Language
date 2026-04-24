namespace Gateway.Core.Interfaces;

public interface ISqlSafetyInterceptor
{
    /// <summary>
    /// Validates the generated SQL against the authorized AST metadata.
    /// </summary>
    bool IsQuerySafe(string generatedSql, string userPrompt, out string errorMessage);
}