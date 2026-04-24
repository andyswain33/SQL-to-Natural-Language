using Gateway.Core.Services;
using System.Text.Json;
using Xunit;

namespace Gateway.Tests;

public class PiiMaskingTests
{
    private readonly DataMaskingService _maskingService;

    public PiiMaskingTests()
    {
        // The service now lives in Core and has zero dependencies!
        _maskingService = new DataMaskingService();
    }

    [Theory]
    [InlineData("alice.smith@enterprise.com", "a***h@enterprise.com")]
    [InlineData("bob.jones@enterprise.com", "b***s@enterprise.com")]
    [InlineData("ab@test.org", "***@test.org")] // Tests the short-prefix edge case
    public void MaskAndSerialize_MaskingEnabled_MasksEmailsCorrectly(string rawEmail, string expectedMask)
    {
        // Arrange
        var rawData = new List<Dictionary<string, object?>>
        {
            new() { { "Email", rawEmail }, { "FirstName", "Test" } }
        };

        // Act
        var jsonResult = _maskingService.MaskAndSerialize(rawData, enableMasking: true);

        // Assert
        Assert.Contains(expectedMask, jsonResult);
        Assert.DoesNotContain(rawEmail, jsonResult);
    }

    [Fact]
    public void MaskAndSerialize_MaskingDisabled_ReturnsRawDataUnchanged()
    {
        // Arrange
        string rawEmail = "top.secret@enterprise.com";
        var rawData = new List<Dictionary<string, object?>>
        {
            new() { { "Email", rawEmail } }
        };

        // Act
        var jsonResult = _maskingService.MaskAndSerialize(rawData, enableMasking: false);

        // Assert
        Assert.Contains(rawEmail, jsonResult);
    }

    [Theory]
    [InlineData("FirstName", "Alice")]
    [InlineData("Department", "IT")]
    [InlineData("ClearanceLevel", 3)]
    public void MaskAndSerialize_NonPiiColumns_AreNotMasked(string columnName, object rawValue)
    {
        // Arrange
        var rawData = new List<Dictionary<string, object?>>
        {
            new() { { columnName, rawValue } }
        };

        // Act
        var jsonResult = _maskingService.MaskAndSerialize(rawData, enableMasking: true);

        // Assert
        var expectedValueString = rawValue.ToString();
        Assert.Contains(expectedValueString!, jsonResult);
    }

    [Fact]
    public void MaskAndSerialize_NullValues_AreHandledSafely()
    {
        // Arrange
        var rawData = new List<Dictionary<string, object?>>
        {
            new() { { "Email", null }, { "Department", "" } }
        };

        // Act
        var jsonResult = _maskingService.MaskAndSerialize(rawData, enableMasking: true);

        // Assert
        Assert.Contains("\"Email\": null", jsonResult);
        Assert.Contains("\"Department\": \"\"", jsonResult);
    }
}