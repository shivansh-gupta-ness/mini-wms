using Microsoft.EntityFrameworkCore;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>TASK-004 AC3 (central audit stamps and Version) and AC4 (stale Version raises a concurrency exception).</summary>
public class AuditAndVersionTests
{
    private static readonly DateTimeOffset CreateTime = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdateTime = new(2026, 3, 2, 9, 30, 0, TimeSpan.Zero);

    // AC3
    [Fact]
    [Trait("AC", "TASK-004.3")]
    public void Adding_an_entity_sets_CreatedAt_and_CreatedBy_from_the_clock_and_current_user()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext(new FakeClock(CreateTime), new FakeCurrentUser("creator"));
        var item = new PersistenceTestItem { Name = "new" };

        context.Items.Add(item);
        context.SaveChanges();

        Assert.Equal(CreateTime, item.CreatedAt);
        Assert.Equal("creator", item.CreatedBy);
    }

    [Fact]
    [Trait("AC", "TASK-004.3")]
    public void Adding_an_entity_sets_Version_1_and_leaves_the_update_fields_empty()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext(new FakeClock(CreateTime), new FakeCurrentUser("creator"));
        var item = new PersistenceTestItem { Name = "new" };

        context.Items.Add(item);
        context.SaveChanges();

        Assert.Equal(1, item.Version);
        Assert.Null(item.UpdatedAt);
        Assert.Null(item.UpdatedBy);
    }

    [Fact]
    [Trait("AC", "TASK-004.3")]
    public void Updating_an_entity_sets_UpdatedAt_and_UpdatedBy_and_keeps_the_created_fields()
    {
        using var database = new TemporaryDatabase();
        var id = SeedItem(database, "before");
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        var item = context.Items.Single(i => i.Id == id);

        item.Name = "after";
        context.SaveChanges();

        Assert.Equal(UpdateTime, item.UpdatedAt);
        Assert.Equal("editor", item.UpdatedBy);
        Assert.Equal(CreateTime, item.CreatedAt);
        Assert.Equal("creator", item.CreatedBy);
    }

    [Fact]
    [Trait("AC", "TASK-004.3")]
    public void Updating_an_entity_increases_Version_from_1_to_2()
    {
        using var database = new TemporaryDatabase();
        var id = SeedItem(database, "before");
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        var item = context.Items.Single(i => i.Id == id);

        item.Name = "after";
        context.SaveChanges();

        Assert.Equal(2, item.Version);
    }

    [Fact]
    [Trait("AC", "TASK-004.3")]
    public void Stamps_and_Version_are_stored_and_read_back_by_a_fresh_context()
    {
        using var database = new TemporaryDatabase();
        var id = SeedItem(database, "before");
        var updater = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        var tracked = updater.Items.Single(i => i.Id == id);
        tracked.Name = "after";
        updater.SaveChanges();

        var stored = database.CreateContext().Items.AsNoTracking().Single(i => i.Id == id);

        Assert.Equal(CreateTime, stored.CreatedAt);
        Assert.Equal("creator", stored.CreatedBy);
        Assert.Equal(UpdateTime, stored.UpdatedAt);
        Assert.Equal("editor", stored.UpdatedBy);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    [Trait("AC", "TASK-004.3")]
    public void Saving_without_changes_does_not_change_Version_or_UpdatedAt()
    {
        using var database = new TemporaryDatabase();
        var id = SeedItem(database, "unchanged");
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        var item = context.Items.Single(i => i.Id == id);

        context.SaveChanges();

        Assert.Equal(1, item.Version);
        Assert.Null(item.UpdatedAt);
        Assert.Null(item.UpdatedBy);
    }

    [Fact]
    [Trait("AC", "TASK-004.3")]
    public async Task SaveChangesAsync_stamps_audit_fields_and_Version_on_add_and_update()
    {
        using var database = new TemporaryDatabase();
        var clock = new FakeClock(CreateTime);
        var user = new FakeCurrentUser("creator");
        var context = database.CreateContext(clock, user);
        var item = new PersistenceTestItem { Name = "async" };
        context.Items.Add(item);
        await context.SaveChangesAsync();
        clock.UtcNow = UpdateTime;
        user.UserName = "editor";

        item.Name = "async changed";
        await context.SaveChangesAsync();

        Assert.Equal(CreateTime, item.CreatedAt);
        Assert.Equal("creator", item.CreatedBy);
        Assert.Equal(UpdateTime, item.UpdatedAt);
        Assert.Equal("editor", item.UpdatedBy);
        Assert.Equal(2, item.Version);
    }

    // AC4
    [Fact]
    [Trait("AC", "TASK-004.4")]
    public void Saving_an_update_from_a_stale_context_raises_DbUpdateConcurrencyException()
    {
        using var database = new TemporaryDatabase();
        var id = SeedItem(database, "original");
        var first = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("first"));
        var stale = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("stale"));
        var firstItem = first.Items.Single(i => i.Id == id);
        var staleItem = stale.Items.Single(i => i.Id == id);
        firstItem.Name = "first update";
        first.SaveChanges();

        staleItem.Name = "stale update";

        Assert.Throws<DbUpdateConcurrencyException>(() => stale.SaveChanges());
    }

    [Fact]
    [Trait("AC", "TASK-004.4")]
    public void After_a_rejected_stale_update_only_the_first_update_is_stored()
    {
        using var database = new TemporaryDatabase();
        var id = SeedItem(database, "original");
        var first = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("first"));
        var stale = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("stale"));
        var firstItem = first.Items.Single(i => i.Id == id);
        var staleItem = stale.Items.Single(i => i.Id == id);
        firstItem.Name = "first update";
        first.SaveChanges();
        staleItem.Name = "stale update";
        Assert.Throws<DbUpdateConcurrencyException>(() => stale.SaveChanges());

        var stored = database.CreateContext().Items.AsNoTracking().Single(i => i.Id == id);

        Assert.Equal("first update", stored.Name);
        Assert.Equal("first", stored.UpdatedBy);
        Assert.Equal(2, stored.Version);
    }

    private static int SeedItem(TemporaryDatabase database, string name)
    {
        var context = database.CreateContext(new FakeClock(CreateTime), new FakeCurrentUser("creator"));
        var item = new PersistenceTestItem { Name = name };
        context.Items.Add(item);
        context.SaveChanges();
        return item.Id;
    }
}
