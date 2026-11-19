namespace MiniWms.Domain.Common;

/// <summary>Shared input checks for entity rules. Messages are written for users.</summary>
internal static class Guard
{
    /// <summary>Returns the trimmed value; throws when it is blank or longer than <paramref name="maxLength"/>.</summary>
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
