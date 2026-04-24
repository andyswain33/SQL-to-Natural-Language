namespace Gateway.Core.Interfaces;

public interface IQueryExecutor
{
    /// <summary>
    /// Executes a validated SQL query and returns raw data dictionaries.
    /// </summary>
    Task<List<Dictionary<string, object?>>> ExecuteRawAsync(string validSql);
}