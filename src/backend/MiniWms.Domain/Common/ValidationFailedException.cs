using System.Collections.ObjectModel;

namespace MiniWms.Domain.Common;

/// <summary>
/// An input value broke a field rule (blank, too long, negative, not a valid choice). Messages are written for
/// users. Field names are the camelCase names of the request field, as they appear in the 400 response.
/// Business-rule conflicts use <see cref="DomainException"/> instead.
/// </summary>
public class ValidationFailedException : Exception
{
    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    public ValidationFailedException(IDictionary<string, string[]> errors)
        : base(BuildMessage(errors))
    {
        Errors = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(errors));
    }

    /// <summary>Messages per field name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private static string BuildMessage(IDictionary<string, string[]> errors) =>
        string.Join(" ", errors.Values.SelectMany(messages => messages));
}
