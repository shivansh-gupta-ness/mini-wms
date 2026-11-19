namespace MiniWms.Application.Common;

/// <summary>The user on whose behalf the current operation runs.</summary>
public interface ICurrentUser
{
    string UserName { get; }
}
