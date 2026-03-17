using Gateway.Infrastructure.Data;
using Xunit;

namespace Gateway.Tests
{
    public class SqlSafetyInterceptorTests
    {
        private readonly SqlSafetyInterceptor _interceptor;

        public SqlSafetyInterceptorTests()
        {
            // The interceptor has no external dependencies, making it beautifully easy to unit test.
            _interceptor = new SqlSafetyInterceptor();
        }

        [Theory]
        [InlineData("SELECT FirstName, LastName FROM vw_ActiveIdentities WHERE ClearanceLevel = 3")]
        [InlineData("WITH CTE AS (SELECT * FROM vw_HighSecurityAccessLogs) SELECT * FROM CTE")]
        [InlineData("select email from VW_ActiveIdentities")] // Case insensitivity check
        [InlineData("  SELECT * FROM vw_ActiveIdentities  ")] // Whitespace check
        public void IsQuerySafe_ValidQueries_ReturnsTrue(string validSql)
        {
            // Act
            bool isSafe = _interceptor.IsQuerySafe(validSql, out string errorMessage);

            // Assert
            Assert.True(isSafe);
            Assert.Empty(errorMessage);
        }

        [Theory]
        // Prepend SELECT to bypass Rule 1, forcing Rule 2 (the Regex scanner) to do the work.
        [InlineData("SELECT * FROM vw_ActiveIdentities; UPDATE vw_ActiveIdentities SET ClearanceLevel = 3", "UPDATE")]
        [InlineData("SELECT * FROM vw_ActiveIdentities; DROP TABLE Identities;", "DROP")]
        [InlineData("SELECT * FROM vw_ActiveIdentities; DELETE FROM vw_HighSecurityAccessLogs", "DELETE")]
        [InlineData("SELECT * FROM vw_ActiveIdentities; INSERT INTO vw_ActiveIdentities (FirstName) VALUES ('Test')", "INSERT")]
        // Removed 'DROP' from the payload so it specifically triggers the 'EXEC' rule
        [InlineData("SELECT * FROM vw_ActiveIdentities; EXEC sp_msforeachtable 'SELECT 1'", "EXEC")]
        [InlineData("SELECT * FROM vw_ActiveIdentities; ALTER VIEW vw_ActiveIdentities AS SELECT * FROM Identities", "ALTER")]
        public void IsQuerySafe_DestructiveKeywords_ReturnsFalse(string maliciousSql, string expectedKeyword)
        {
            // Act
            bool isSafe = _interceptor.IsQuerySafe(maliciousSql, out string errorMessage);

            // Assert
            Assert.False(isSafe);
            Assert.Contains($"'{expectedKeyword}'", errorMessage);
        }

        [Fact]
        public void IsQuerySafe_TargetsBaseTableInsteadOfView_ReturnsFalse()
        {
            // Arrange - Valid SELECT, but targets the raw 'Identities' table directly
            string sql = "SELECT * FROM Identities";

            // Act
            bool isSafe = _interceptor.IsQuerySafe(sql, out string errorMessage);

            // Assert
            Assert.False(isSafe);
            Assert.Contains("read-only view", errorMessage);
        }

        [Fact]
        public void IsQuerySafe_DoesNotStartWithSelect_ReturnsFalse()
        {
            // Arrange
            string sql = "TRUNCATE TABLE AccessAttempts";

            // Act
            bool isSafe = _interceptor.IsQuerySafe(sql, out string errorMessage);

            // Assert
            Assert.False(isSafe);
            Assert.Contains("SELECT statement or a CTE", errorMessage);
        }

        [Fact]
        public void IsQuerySafe_EmptyQuery_ReturnsFalse()
        {
            // Act
            bool isSafe = _interceptor.IsQuerySafe("   ", out string errorMessage);

            // Assert
            Assert.False(isSafe);
            Assert.Contains("empty query", errorMessage);
        }
    }
}