using System.Reflection;
using MiniWms.Domain.Common;
using MiniWms.Domain.Products;

namespace MiniWms.Domain.Tests.Products;

public class ProductTests
{
    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_trims_text_and_starts_active()
    {
        var product = Product.Create("  SKU-1 ", " Widget ", " EA ", 5);

        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("Widget", product.Name);
        Assert.Equal("EA", product.UnitOfMeasure);
        Assert.Equal(5, product.ReorderLevel);
        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [Trait("AC", "TASK-005.6")]
    public void Create_accepts_a_reorder_level_of_zero_or_more(int level)
    {
        var product = Product.Create("SKU-1", "Widget", "EA", level);

        Assert.Equal(level, product.ReorderLevel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [Trait("AC", "TASK-005.6")]
    public void Create_rejects_a_negative_reorder_level(int level)
    {
        Assert.Throws<ValidationFailedException>(() => Product.Create("SKU-1", "Widget", "EA", level));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("AC", "TASK-005.6")]
    public void Create_requires_sku_name_and_unit_of_measure(string? blank)
    {
        Assert.Throws<ValidationFailedException>(() => Product.Create(blank, "Widget", "EA", 0));
        Assert.Throws<ValidationFailedException>(() => Product.Create("SKU-1", blank, "EA", 0));
        Assert.Throws<ValidationFailedException>(() => Product.Create("SKU-1", "Widget", blank, 0));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Create_enforces_the_maximum_lengths_40_150_and_20()
    {
        var atLimit = Product.Create(new string('S', 40), new string('N', 150), new string('U', 20), 0);

        Assert.Equal(40, atLimit.Sku.Length);
        Assert.Throws<ValidationFailedException>(() => Product.Create(new string('S', 41), "Widget", "EA", 0));
        Assert.Throws<ValidationFailedException>(() => Product.Create("SKU-1", new string('N', 151), "EA", 0));
        Assert.Throws<ValidationFailedException>(() => Product.Create("SKU-1", "Widget", new string('U', 21), 0));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Sku_has_no_public_setter_and_Update_has_no_sku_parameter()
    {
        var setter = typeof(Product).GetProperty(nameof(Product.Sku))!.SetMethod;
        var update = typeof(Product).GetMethod(nameof(Product.Update))!;

        Assert.True(setter is null || !setter.IsPublic);
        Assert.DoesNotContain(update.GetParameters(), p => p.Name!.Contains("sku", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Update_changes_name_unit_and_reorder_level_but_never_the_sku()
    {
        var product = Product.Create("SKU-1", "Widget", "EA", 5);

        product.Update(" Gadget ", "BOX", 0);

        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("Gadget", product.Name);
        Assert.Equal("BOX", product.UnitOfMeasure);
        Assert.Equal(0, product.ReorderLevel);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Update_with_a_negative_reorder_level_is_rejected_and_changes_nothing()
    {
        var product = Product.Create("SKU-1", "Widget", "EA", 5);

        Assert.Throws<ValidationFailedException>(() => product.Update("Gadget", "BOX", -1));

        Assert.Equal("Widget", product.Name);
        Assert.Equal("EA", product.UnitOfMeasure);
        Assert.Equal(5, product.ReorderLevel);
    }

    [Theory]
    [InlineData(null, "EA")]
    [InlineData("  ", "EA")]
    [InlineData("Gadget", null)]
    [InlineData("Gadget", "")]
    [Trait("AC", "TASK-005.6")]
    public void Update_requires_name_and_unit_of_measure_and_changes_nothing_on_failure(string? name, string? unit)
    {
        var product = Product.Create("SKU-1", "Widget", "EA", 5);

        Assert.Throws<ValidationFailedException>(() => product.Update(name, unit, 9));

        Assert.Equal("Widget", product.Name);
        Assert.Equal("EA", product.UnitOfMeasure);
        Assert.Equal(5, product.ReorderLevel);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_is_rejected_while_total_on_hand_is_above_zero(int onHand)
    {
        var product = Product.Create("SKU-1", "Widget", "EA", 0);

        Assert.Throws<DomainException>(() => product.Deactivate(onHand));
        Assert.True(product.IsActive);
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_works_with_no_stock_and_Activate_reverses_it()
    {
        var product = Product.Create("SKU-1", "Widget", "EA", 0);

        product.Deactivate(0);
        var afterDeactivate = product.IsActive;
        product.Activate();

        Assert.False(afterDeactivate);
        Assert.True(product.IsActive);
    }
}

/// <summary>TASK-005 AC7: the Domain proving tests carry their AC trait.</summary>
public class TraitCoverageTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [Trait("AC", "TASK-005.7")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac)
    {
        var expected = $"TASK-005.{ac}";

        var tagged = typeof(TraitCoverageTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

        Assert.True(tagged > 0, $"No test is tagged [Trait(\"AC\", \"{expected}\")].");
    }

    [Fact]
    [Trait("AC", "TASK-005.6")]
    public void Deactivate_rejects_a_negative_stock_total_as_an_invalid_argument()
    {
        var product = Product.Create("SKU-1", "Widget", "EA", 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.Deactivate(-1));
        Assert.True(product.IsActive);
    }
}
