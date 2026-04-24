using System.Text.Json;

namespace Gateway.Core.Services;

public class DataMaskingService
{
    public string MaskAndSerialize(List<Dictionary<string, object?>> rawData, bool enableMasking)
    {
        if (!enableMasking)
        {
            return JsonSerializer.Serialize(rawData, new JsonSerializerOptions { WriteIndented = true });
        }

        var maskedList = new List<Dictionary<string, object?>>(rawData.Count);

        foreach (var row in rawData)
        {
            var maskedRow = new Dictionary<string, object?>();
            foreach (var kvp in row)
            {
                maskedRow[kvp.Key] = MaskIfPii(kvp.Key, kvp.Value);
            }
            maskedList.Add(maskedRow);
        }

        return JsonSerializer.Serialize(maskedList, new JsonSerializerOptions { WriteIndented = true });
    }

    private object? MaskIfPii(string columnName, object? value)
    {
        if (value == null) return null;

        string stringValue = value.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(stringValue)) return string.Empty;

        if (columnName.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            return MaskEmail(stringValue);
        }

        return value;
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "[REDACTED]";

        int atIndex = email.IndexOf('@');
        if (atIndex < 0) return "[REDACTED]";

        if (atIndex <= 2)
        {
            return string.Concat("***", email.AsSpan(atIndex));
        }

        return string.Create(email.Length, (email, atIndex), (span, state) =>
        {
            ReadOnlySpan<char> originalSpan = state.email.AsSpan();
            ReadOnlySpan<char> localPart = originalSpan.Slice(0, state.atIndex);
            ReadOnlySpan<char> domainPart = originalSpan.Slice(state.atIndex);

            span[0] = localPart[0];
            for (int i = 1; i < localPart.Length - 1; i++) span[i] = '*';
            span[localPart.Length - 1] = localPart[^1];
            domainPart.CopyTo(span.Slice(localPart.Length));
        });
    }
}