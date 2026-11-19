using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Domain.Tests.Locations;

public class ZoneTests
{
    [Theory]
    [InlineData(ZoneType.Receiving)]
    [InlineData(ZoneType.Storage)]
    [InlineData(ZoneType.Shipping)]
    [Trait("AC", "TASK-005.6")]
    public void Create_accepts_each_allowed_type_and_is_active(ZoneType type)
    {
        var zone = Zone.Create(1, "Z1", "Zone", type);

        Assert.Equal(type, zone.Type);
        Assert.True(zone.IsActive);
        Assert.Equal(1, zone.WarehouseId);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(99)]
    [Trait("AC", "TASK-005.5")]
    [Trait("AC", "TASK-005.6")]
    public void Create_rejects_a_type_that_is_not_Receiving_Storage_or_Shipping(int value)
    {
        Assert.Throws<DomainException>(() => Zone.Create(1, "Z1", "Zone", (ZoneType)value));
    }

    [Fact]
    [Trait("AC", "TASK-005.5")]
    [Trait("AC", "TASK-005.6")]
    public void ChangeType_rejects_an_undefined_type_even_when_the_zone_has_no_bins()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        Assert.Throws<DomainException>(() => zone.ChangeType((ZoneType)99, hasBins: false));
        Assert.Equal(ZoneType.Storage, zone.Type);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void ChangeType_changes_the_type_when_the_zone_has_no_bins()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        zone.ChangeType(ZoneType.Shipping, hasBins: false);

        Assert.Equal(ZoneType.Shipping, zone.Type);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void ChangeType_to_a_different_type_is_rejected_once_the_zone_has_bins()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        Assert.Throws<DomainException>(() => zone.ChangeType(ZoneType.Shipping, hasBins: true));
        Assert.Equal(ZoneType.Storage, zone.Type);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void ChangeType_to_the_same_type_is_allowed_when_the_zone_has_bins()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        zone.ChangeType(ZoneType.Storage, hasBins: true);

        Assert.Equal(ZoneType.Storage, zone.Type);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_is_rejected_while_the_zone_has_active_bins()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        Assert.Throws<DomainException>(() => zone.Deactivate(hasActiveBins: true));
        Assert.True(zone.IsActive);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_works_without_active_bins_and_Activate_reverses_it()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        zone.Deactivate(hasActiveBins: false);
        var afterDeactivate = zone.IsActive;
        zone.Activate();

        Assert.False(afterDeactivate);
        Assert.True(zone.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("AC", "TASK-005.6")]
    public void Create_requires_a_code_and_a_name(string? blank)
    {
        Assert.Throws<DomainException>(() => Zone.Create(1, blank, "Zone", ZoneType.Storage));
        Assert.Throws<DomainException>(() => Zone.Create(1, "Z1", blank, ZoneType.Storage));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_trims_code_and_name()
    {
        var zone = Zone.Create(1, "  Z1  ", "  Zone one ", ZoneType.Storage);

        Assert.Equal("Z1", zone.Code);
        Assert.Equal("Zone one", zone.Name);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_enforces_the_maximum_lengths_of_20_for_code_and_100_for_name()
    {
        var atLimit = Zone.Create(1, new string('C', 20), new string('N', 100), ZoneType.Storage);

        Assert.Equal(20, atLimit.Code.Length);
        Assert.Equal(100, atLimit.Name.Length);
        Assert.Throws<DomainException>(() => Zone.Create(1, new string('C', 21), "Zone", ZoneType.Storage));
        Assert.Throws<DomainException>(() => Zone.Create(1, "Z1", new string('N', 101), ZoneType.Storage));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Length_limit_applies_after_trimming()
    {
        var zone = Zone.Create(1, "  " + new string('C', 20) + "  ", "Zone", ZoneType.Storage);

        Assert.Equal(20, zone.Code.Length);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Rename_changes_the_name_trimmed_and_keeps_the_code()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        zone.Rename("  New name ");

        Assert.Equal("New name", zone.Name);
        Assert.Equal("Z1", zone.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [Trait("AC", "TASK-005.6")]
    public void Rename_rejects_a_blank_name_and_keeps_the_old_one(string? blank)
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        Assert.Throws<DomainException>(() => zone.Rename(blank));
        Assert.Equal("Zone", zone.Name);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Rename_rejects_a_name_longer_than_100_characters()
    {
        var zone = Zone.Create(1, "Z1", "Zone", ZoneType.Storage);

        Assert.Throws<DomainException>(() => zone.Rename(new string('N', 101)));
    }
}
