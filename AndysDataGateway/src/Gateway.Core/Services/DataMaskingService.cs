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

        // We want: FirstChar + "***" + LastChar + "@domain.com"
        // The domainPart length includes the '@' symbol.
        int domainLength = email.Length - atIndex;
        int newLength = 5 + domainLength; // 1 (first) + 3 (***) + 1 (last) = 5

        return string.Create(newLength, (email, atIndex), (span, state) =>
        {
            ReadOnlySpan<char> originalSpan = state.email.AsSpan();
            ReadOnlySpan<char> localPart = originalSpan.Slice(0, state.atIndex);
            ReadOnlySpan<char> domainPart = originalSpan.Slice(state.atIndex);

            span[0] = localPart[0];
            span[1] = '*';
            span[2] = '*';
            span[3] = '*';
            span[4] = localPart[^1]; // Last char of the local part

            domainPart.CopyTo(span.Slice(5));
        });
    }
}