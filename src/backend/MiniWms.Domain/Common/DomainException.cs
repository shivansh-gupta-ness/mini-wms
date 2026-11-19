namespace MiniWms.Domain.Common;

/// <summary>
/// A business rule was broken. The API sends the message to the client as the 409 <c>detail</c>, so it must be fixed
/// text written for users: never include secrets, internal state or data the caller may not see.
/// </summary>
public class DomainException(string message) : Exception(message);
