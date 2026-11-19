using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Domain.Tests.Locations;

public class BinTests
{
    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_trims_code_and_name_and_starts_active()
    {
        var bin = Bin.Create(4, "  A-01-01 ", " First bin ");

        Assert.Equal(4, bin.ZoneId);
        Assert.Equal("A-01-01", bin.Code);
        Assert.Equal("First bin", bin.Name);
        Assert.True(bin.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("AC", "TASK-005.6")]
    public void Create_requires_a_code_and_a_name(string? blank)
    {
        Assert.Throws<ValidationFailedException>(() => Bin.Create(1, blank, "Bin"));
        Assert.Throws<ValidationFailedException>(() => Bin.Create(1, "A-01", blank));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_enforces_the_maximum_lengths_of_30_for_code_and_100_for_name()
    {
        var atLimit = Bin.Create(1, new string('C', 30), new string('N', 100));

        Assert.Equal(30, atLimit.Code.Length);
        Assert.Throws<ValidationFailedException>(() => Bin.Create(1, new string('C', 31), "Bin"));
        Assert.Throws<ValidationFailedException>(() => Bin.Create(1, "A-01", new string('N', 101)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_is_rejected_while_stock_on_hand_is_above_zero(int onHand)
    {
        var bin = Bin.Create(1, "A-01", "Bin");

        Assert.Throws<DomainException>(() => bin.Deactivate(onHand));
        Assert.True(bin.IsActive);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_works_with_no_stock_and_Activate_reverses_it()
    {
        var bin = Bin.Create(1, "A-01", "Bin");

        bin.Deactivate(0);
        var afterDeactivate = bin.IsActive;
        bin.Activate();

        Assert.False(afterDeactivate);
        Assert.True(bin.IsActive);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Rename_changes_the_name_and_keeps_the_code()
    {
        var bin = Bin.Create(1, "A-01", "Bin");

        bin.Rename(" Renamed ");

        Assert.Equal("Renamed", bin.Name);
        Assert.Equal("A-01", bin.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [Trait("AC", "TASK-005.6")]
    public void Rename_rejects_a_blank_name(string? blank)
    {
        var bin = Bin.Create(1, "A-01", "Bin");

        Assert.Throws<ValidationFailedException>(() => bin.Rename(blank));
        Assert.Equal("Bin", bin.Name);
    }
}

public class WarehouseTests
{
    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_trims_code_and_name_and_starts_active()
    {
        var warehouse = Warehouse.Create(" MAIN ", " Main warehouse ");

        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main warehouse", warehouse.Name);
        Assert.True(warehouse.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("AC", "TASK-005.6")]
    public void Create_requires_a_code_and_a_name(string? blank)
    {
        Assert.Throws<ValidationFailedException>(() => Warehouse.Create(blank, "Name"));
        Assert.Throws<ValidationFailedException>(() => Warehouse.Create("MAIN", blank));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_enforces_the_maximum_lengths_of_20_for_code_and_100_for_name()
    {
        var atLimit = Warehouse.Create(new string('C', 20), new string('N', 100));

        Assert.Equal(20, atLimit.Code.Length);
        Assert.Throws<ValidationFailedException>(() => Warehouse.Create(new string('C', 21), "Name"));
        Assert.Throws<ValidationFailedException>(() => Warehouse.Create("MAIN", new string('N', 101)));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Rename_changes_the_name_and_rejects_a_blank_one()
    {
        var warehouse = Warehouse.Create("MAIN", "Main warehouse");

        warehouse.Rename("Central");

        Assert.Equal("Central", warehouse.Name);
        Assert.Throws<ValidationFailedException>(() => warehouse.Rename(" "));
        Assert.Equal("Central", warehouse.Name);
        Assert.Equal("MAIN", warehouse.Code);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_rejects_a_negative_stock_total_as_an_invalid_argument()
    {
        var bin = Bin.Create(1, "A-01", "Bin");

        Assert.Throws<ArgumentOutOfRangeException>(() => bin.Deactivate(-5));
        Assert.True(bin.IsActive);
    }
}
