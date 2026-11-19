namespace MiniWms.Application.Common;

/// <summary>Source of the current UTC time, so that audit stamps can be tested.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
