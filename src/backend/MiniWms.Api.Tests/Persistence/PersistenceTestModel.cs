using Microsoft.EntityFrameworkCore;
using MiniWms.Application.Common;
using MiniWms.Domain.Common;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>Test-only entity: there are no module tables yet (TASK-005), so the audit conventions need one to act on.</summary>
internal sealed class PersistenceTestItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Test-only context: the production <see cref="AppDbContext"/> plus one test-only table.</summary>
internal sealed class PersistenceTestDbContext(
    DbContextOptions<AppDbContext> options, IClock clock, ICurrentUser currentUser)
    : AppDbContext(options, clock, currentUser)
{
    public DbSet<PersistenceTestItem> Items => Set<PersistenceTestItem>();
}

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

internal sealed class FakeCurrentUser(string userName) : ICurrentUser
{
    public string UserName { get; set; } = userName;
}
