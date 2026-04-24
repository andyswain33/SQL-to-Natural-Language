using Gateway.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace Gateway.Tests;

public class PiiMaskingTests
{
    private readonly SqlExecutionService _executionService;

    public PiiMaskingTests()
    {
        // Build a "dummy" configuration object to satisfy the constructor
        var inMemorySettings = new Dictionary<string, string?> {
            {"ConnectionStrings:DefaultConnection", "Server=dummy;Database=dummy;"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _executionService = new SqlExecutionService(configuration);
    }

    [Theory]
    [InlineData("Email", "alice.smith@enterprise.com", "a***h@enterprise.com")]
    [InlineData("email", "bob.jones@enterprise.com", "b***s@enterprise.com")]
    [InlineData("EMAIL_ADDRESS", "charlie.brown@test.org", "c***n@test.org")]
    [InlineData("ContactEmail", "d.trump@gov.us", "d***p@gov.us")]
    public void MaskIfPii_EmailColumns_SuccessfullyMasksData(string columnName, string rawEmail, string expectedMask)
    {
        // Act
        var result = _executionService.MaskIfPii(columnName, rawEmail);

        // Assert
        Assert.Equal(expectedMask, result);
    }

    [Theory]
    [InlineData("FirstName", "Alice")]
    [InlineData("LastName", "Smith")]
    [InlineData("Department", "IT")]
    [InlineData("ClearanceLevel", 3)]
    public void MaskIfPii_NonPiiColumns_ReturnsRawData(string columnName, object rawValue)
    {
        // Act
        var result = _executionService.MaskIfPii(columnName, rawValue);

        // Assert
        // By converting both sides to strings, we eliminate any int/string/long boxing conflicts
        Assert.Equal(rawValue?.ToString(), result?.ToString());
    }

    [Fact]
    public void MaskIfPii_NullValues_ReturnsNull()
    {
        // Act
        var result = _executionService.MaskIfPii("Email", null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MaskIfPii_EmptyEmailString_ReturnsEmptyString()
    {
        // Act
        var result = _executionService.MaskIfPii("Email", "");

        // Assert
        Assert.Equal("", result);
    }
}