namespace MiniWms.Domain.Common;

/// <summary>A business rule was broken. The message is written for users and may be shown to them.</summary>
public class DomainException(string message) : Exception(message);
