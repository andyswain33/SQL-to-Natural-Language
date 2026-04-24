using Gateway.Core.Mapping;
using Gateway.Infrastructure.Data;
using Xunit;

namespace Gateway.Tests;

public class SqlSafetyInterceptorTests
{
    private readonly SqlSafetyInterceptor _interceptor;

    public SqlSafetyInterceptorTests()
    {
        // Wire up the actual MetadataMapper so we can test the context boundaries
        var mapper = new MetadataMapper();
        _interceptor = new SqlSafetyInterceptor(mapper);
    }

    [Fact]
    public void IsQuerySafe_ValidSelectWithinContext_ReturnsTrue()
    {
        // Arrange: The prompt implies checking users (Identities)
        string prompt = "Show me all IT staff";
        string sql = "SELECT FirstName, LastName FROM vw_ActiveIdentities WHERE Department = 'IT'";

        // Act
        bool isSafe = _interceptor.IsQuerySafe(sql, prompt, out string errorMessage);

        // Assert
        Assert.True(isSafe);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void IsQuerySafe_UnauthorizedView_ReturnsFalse()
    {
        // Arrange: The prompt is explicitly about identities ('employees')
        // Therefore, MetadataMapper will ONLY authorize vw_ActiveIdentities.
        string prompt = "Show me all IT employees";

        // The SQL maliciously targets SecurityLogs, which was NOT authorized by the prompt.
        string sql = "SELECT * FROM vw_HighSecurityAccessLogs";

        // Act
        bool isSafe = _interceptor.IsQuerySafe(sql, prompt, out string errorMessage);

        // Assert
        Assert.False(isSafe);
        Assert.Contains("Security Violation", errorMessage);
        Assert.Contains("vw_HighSecurityAccessLogs", errorMessage);
    }

    [Fact]
    public void IsQuerySafe_DestructiveDmlCommand_ReturnsFalse()
    {
        // Arrange
        string prompt = "Show me all IT staff";
        string sql = "UPDATE vw_ActiveIdentities SET ClearanceLevel = 5 WHERE Department = 'IT'";

        // Act
        bool isSafe = _interceptor.IsQuerySafe(sql, prompt, out string errorMessage);

        // Assert
        Assert.False(isSafe);
        Assert.Contains("Only SELECT statements are permitted", errorMessage);
    }

    [Fact]
    public void IsQuerySafe_MultipleStatements_ReturnsFalse()
    {
        // Arrange: A classic injection attempt
        string prompt = "Show me all IT staff";
        string sql = "SELECT * FROM vw_ActiveIdentities; DROP TABLE vw_ActiveIdentities;";

        // Act
        bool isSafe = _interceptor.IsQuerySafe(sql, prompt, out string errorMessage);

        // Assert
        Assert.False(isSafe);
        Assert.Contains("Only single-statement batches are allowed", errorMessage);
    }

    [Fact]
    public void IsQuerySafe_InvalidSqlSyntax_ReturnsFalse()
    {
        // Arrange: The LLM hallucinated terrible SQL
        string prompt = "Show me all IT staff";
        string sql = "SELECT * FROM WHERE AND ORDER BY WHAT";

        // Act
        bool isSafe = _interceptor.IsQuerySafe(sql, prompt, out string errorMessage);

        // Assert
        Assert.False(isSafe);
        Assert.Contains("SQL parsing failed. Invalid syntax", errorMessage);
    }

    [Fact]
    public void IsQuerySafe_BaseTableAccess_ReturnsFalse()
    {
        // Arrange: Trying to access the physical 'Identities' table instead of the view
        string prompt = "Show me all IT staff";
        string sql = "SELECT * FROM Identities";

        // Act
        bool isSafe = _interceptor.IsQuerySafe(sql, prompt, out string errorMessage);

        // Assert
        Assert.False(isSafe);
        Assert.Contains("Security Violation", errorMessage);
        Assert.Contains("Identities", errorMessage);
    }
}