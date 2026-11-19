using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Domain.Products;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>TASK-006 AC6 (client version applied through OriginalValue) and AC2 (sort and page translate to SQLite).</summary>
public class VersionedUpdateTests
{
    private static readonly DateTimeOffset UpdateTime = new(2026, 3, 2, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    [Trait("AC", "TASK-006.6")]
    public void An_update_with_a_stale_client_version_raises_DbUpdateConcurrencyException_and_stores_nothing()
    {
        using var database = new TemporaryDatabase();
        var id = SeedProduct(database, "SKU-1", "original");
        ChangeName(database, id, "changed by someone else"); // stored version is now 2
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("late"));
        var product = context.Products.Single(p => p.Id == id);
        product.Update("late edit", "EA", 0);
        context.UseClientVersion(product, 1);

        Assert.Throws<DbUpdateConcurrencyException>(() => context.SaveChanges());

        var stored = database.CreateContext().Products.AsNoTracking().Single(p => p.Id == id);
        Assert.Equal("changed by someone else", stored.Name);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    [Trait("AC", "TASK-006.6")]
    public void An_update_with_the_current_client_version_succeeds_and_Version_becomes_client_plus_one()
    {
        using var database = new TemporaryDatabase();
        var id = SeedProduct(database, "SKU-1", "original");
        ChangeName(database, id, "second"); // stored version is now 2
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        var product = context.Products.Single(p => p.Id == id);
        product.Update("third", "EA", 0);
        context.UseClientVersion(product, 2);

        context.SaveChanges();

        var stored = database.CreateContext().Products.AsNoTracking().Single(p => p.Id == id);
        Assert.Equal("third", stored.Name);
        Assert.Equal(3, stored.Version);
        Assert.Equal("editor", stored.UpdatedBy);
    }

    [Fact]
    [Trait("AC", "TASK-006.6")]
    public void UseClientVersion_with_no_changed_value_still_writes_and_increments_the_version()
    {
        using var database = new TemporaryDatabase();
        var id = SeedProduct(database, "SKU-1", "original");
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        var product = context.Products.Single(p => p.Id == id);
        context.UseClientVersion(product, 1);

        context.SaveChanges();

        Assert.Equal(2, database.CreateContext().Products.AsNoTracking().Single(p => p.Id == id).Version);
    }

    [Fact]
    [Trait("AC", "TASK-006.6")]
    public void Setting_Version_directly_is_the_documented_bypass_and_a_stale_write_is_accepted()
    {
        using var database = new TemporaryDatabase();
        var id = SeedProduct(database, "SKU-1", "original");
        ChangeName(database, id, "second"); // stored version is now 2
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("late"));
        var product = context.Products.Single(p => p.Id == id);
        product.Update("overwrites silently", "EA", 0);
        product.Version = 1; // stale, but ApplyAudit overwrites it from the loaded original value

        context.SaveChanges();

        var stored = database.CreateContext().Products.AsNoTracking().Single(p => p.Id == id);
        Assert.Equal("overwrites silently", stored.Name);
        Assert.Equal(3, stored.Version);
    }

    [Fact]
    [Trait("AC", "TASK-006.6")]
    public void UseClientVersion_closes_the_bypass_for_the_same_stale_write()
    {
        using var database = new TemporaryDatabase();
        var id = SeedProduct(database, "SKU-1", "original");
        ChangeName(database, id, "second");
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("late"));
        var product = context.Products.Single(p => p.Id == id);
        product.Update("overwrites silently", "EA", 0);
        context.UseClientVersion(product, 1);

        Assert.Throws<DbUpdateConcurrencyException>(() => context.SaveChanges());

        Assert.Equal("second", database.CreateContext().Products.AsNoTracking().Single(p => p.Id == id).Name);
    }

    // AC2: prove sort and page translate to SQL on SQLite, using the throwaway entity and the Product set.
    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Sort_by_name_and_minus_name_translate_on_SQLite_with_Id_as_tie_breaker()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();
        context.Items.AddRange(
            new PersistenceTestItem { Name = "pear" },
            new PersistenceTestItem { Name = "apple" },
            new PersistenceTestItem { Name = "pear" });
        context.SaveChanges();
        var fields = new SortFields<PersistenceTestItem>().Add("name", i => i.Name);

        var ascending = context.Items.ApplySort(SortParser.Parse("name", fields, out _)).Select(i => i.Id).ToList();
        var descending = context.Items.ApplySort(SortParser.Parse("-name", fields, out _)).Select(i => i.Id).ToList();
        var unsorted = context.Items.ApplySort(null).Select(i => i.Id).ToList();

        Assert.Equal([2, 1, 3], ascending);
        Assert.Equal([1, 3, 2], descending);
        Assert.Equal([1, 2, 3], unsorted);
    }

    [Fact]
    [Trait("AC", "TASK-006.3")]
    public void ToPagedResult_counts_all_rows_and_slices_the_page_on_SQLite()
    {
        using var database = new TemporaryDatabase();
        SeedProduct(database, "SKU-1", "bolt");
        SeedProduct(database, "SKU-2", "nut");
        SeedProduct(database, "SKU-3", "screw");
        var fields = new SortFields<Product>().Add("name", p => p.Name).Add("reorderLevel", p => p.ReorderLevel);
        var sort = SortParser.Parse("-name", fields, out _);

        var page = database.CreateContext().Products.AsNoTracking().ApplySort(sort)
            .ToPagedResult(new PagedQuery(2, 2));

        Assert.Equal(["bolt"], page.Items.Select(p => p.Name));
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Sorting_by_an_integer_property_translates_on_SQLite()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();
        context.Products.AddRange(
            Product.Create("A", "a", "EA", 5), Product.Create("B", "b", "EA", 1), Product.Create("C", "c", "EA", 3));
        context.SaveChanges();
        var fields = new SortFields<Product>().Add("reorderLevel", p => p.ReorderLevel);

        var skus = context.Products.AsNoTracking()
            .ApplySort(SortParser.Parse("-reorderLevel", fields, out _)).Select(p => p.Sku).ToList();

        Assert.Equal(["A", "C", "B"], skus);
    }

    // AC7
    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [Trait("AC", "TASK-006.7")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac) =>
        Assert.True(TaggedCount(typeof(VersionedUpdateTests).Assembly, $"TASK-006.{ac}") > 0,
            $"No test is tagged [Trait(\"AC\", \"TASK-006.{ac}\")].");

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(5)]
    [Trait("AC", "TASK-006.7")]
    public void This_assembly_has_proving_tests_for_the_api_criteria(int ac) =>
        Assert.True(TaggedCount(typeof(VersionedUpdateTests).Assembly, $"TASK-006.{ac}") > 0,
            $"No test is tagged [Trait(\"AC\", \"TASK-006.{ac}\")].");

    private static int TaggedCount(Assembly assembly, string expected) =>
        assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

    private static int SeedProduct(TemporaryDatabase database, string sku, string name)
    {
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("creator"));
        var product = Product.Create(sku, name, "EA", 0);
        context.Products.Add(product);
        context.SaveChanges();
        return product.Id;
    }

    private static void ChangeName(TemporaryDatabase database, int id, string name)
    {
        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("first"));
        context.Products.Single(p => p.Id == id).Update(name, "EA", 0);
        context.SaveChanges();
    }

    [Fact]
    [Trait("AC", "TASK-006.3")]
    public async Task ToPagedResultAsync_counts_and_slices_without_blocking()
    {
        using var database = new TemporaryDatabase();
        SeedProduct(database, "SKU-1", "bolt");
        SeedProduct(database, "SKU-2", "nut");
        SeedProduct(database, "SKU-3", "screw");

        var page = await database.CreateContext().Products.AsNoTracking().ApplySort(null)
            .ToPagedResultAsync(new PagedQuery(2, 2));

        Assert.Equal(["SKU-3"], page.Items.Select(p => p.Sku));
        Assert.Equal(3, page.TotalItems);
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public void Sorting_by_a_DateTimeOffset_property_translates_on_SQLite()
    {
        using var database = new TemporaryDatabase();
        SeedProduct(database, "SKU-1", "bolt");
        SeedProduct(database, "SKU-2", "nut");
        var fields = new SortFields<Product>().Add("createdAt", p => p.CreatedAt);

        var ids = database.CreateContext().Products.AsNoTracking()
            .ApplySort(SortParser.Parse("-createdAt", fields, out _)).Select(p => p.Sku).ToList();

        Assert.Equal(2, ids.Count);
    }
}
