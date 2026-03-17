using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gateway.Infrastructure.Data
{
    public class SqlExecutionService
    {
        private readonly string _connectionString;

        public SqlExecutionService(IConfiguration configuration)
        {
            // Pulls from appsettings.json or User Secrets. 
            // We'll provide a fallback matching your Docker container setup for the demo.
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost,1433;User Id=sa;Password=SuperSecurePassword123!;TrustServerCertificate=True;";
        }

        public async Task<string> ExecuteAndMaskAsync(string validSql, bool enableMasking)
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

                    // Apply the mask ONLY if the demo flag is set to true
                    if (enableMasking)
                    {
                        row[columnName] = MaskIfPii(columnName, rawValue);
                    }
                    else
                    {
                        // Pass the raw, vulnerable data through (for the demo)
                        row[columnName] = rawValue == DBNull.Value ? null : rawValue;
                    }
                }
                results.Add(row);
            }

            return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Enterprise PII Masking Logic
        /// </summary>
        public object? MaskIfPii(string columnName, object? value)
        {
            // 1. Handle DBNull and standard nulls immediately
            if (value == DBNull.Value || value == null)
                return null;

            // 2. Safely convert to string and handle empty/whitespace
            string stringValue = value.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(stringValue))
                return string.Empty;

            // Rule 1: Mask based on known column names
            if (columnName.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                return MaskEmail(stringValue);
            }

            // Rule 2: Mask based on known sensitive fields (e.g., SSN, Phone)
            // if (columnName.Contains("SSN", StringComparison.OrdinalIgnoreCase)) { ... }

            // If no rules hit, return the original raw value
            return value;
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "[REDACTED]";

            var parts = email.Split('@');
            var localPart = parts[0];
            var domainPart = parts[1];

            if (localPart.Length <= 2)
                return $"***@{domainPart}";

            // Turns "alice.smith@enterprise.com" into "a***h@enterprise.com"
            return $"{localPart[0]}***{localPart[^1]}@{domainPart}";
        }
    }
}