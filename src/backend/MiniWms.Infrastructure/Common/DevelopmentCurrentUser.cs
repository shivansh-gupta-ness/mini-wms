using MiniWms.Application.Common;

namespace MiniWms.Infrastructure.Common;

/// <summary>Fixed user until authentication exists (ADR-004).</summary>
public sealed class DevelopmentCurrentUser : ICurrentUser
{
    public string UserName => "dev-admin";
}
