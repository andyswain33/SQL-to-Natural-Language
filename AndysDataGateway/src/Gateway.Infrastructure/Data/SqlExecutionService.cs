using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Gateway.Core.Interfaces;

namespace Gateway.Infrastructure.Data;

public class SqlExecutionService : IQueryExecutor
{
    private readonly string _connectionString;

    public SqlExecutionService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;User Id=sa;Password=SuperSecurePassword123!;TrustServerCertificate=True;";
    }

    public async Task<List<Dictionary<string, object?>>> ExecuteRawAsync(string validSql)
    {
        var results = new List<Dictionary<string, object?>>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(validSql, connection);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var rawValue = reader.GetValue(i);

                // Strictly data retrieval. No masking business logic here.
                row[columnName] = rawValue == DBNull.Value ? null : rawValue;
            }
            results.Add(row);
        }

        return results;
    }
}