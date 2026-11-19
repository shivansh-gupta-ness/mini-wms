namespace MiniWms.Domain.Common;

/// <summary>Shared input checks for entity rules. Messages are written for users.</summary>
internal static class Guard
{
    /// <summary>
    /// Returns the trimmed value; throws <see cref="ValidationFailedException"/> when it is blank or longer than
    /// <paramref name="maxLength"/>. <paramref name="field"/> is the camelCase request field name;
    /// <paramref name="label"/> is the name shown in the message.
    /// </summary>
    public static string RequiredText(string? value, string field, string label, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationFailedException(field, $"{label} is required.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new ValidationFailedException(field, $"{label} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
