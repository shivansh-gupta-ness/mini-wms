using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWms.Domain.Products;

namespace MiniWms.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_Sku_Length", $"length(\"Sku\") <= {Product.SkuMaxLength}");
            t.HasCheckConstraint("CK_Products_Name_Length", $"length(\"Name\") <= {Product.NameMaxLength}");
            t.HasCheckConstraint(
                "CK_Products_UnitOfMeasure_Length", $"length(\"UnitOfMeasure\") <= {Product.UnitOfMeasureMaxLength}");
            t.HasCheckConstraint("CK_Products_ReorderLevel", "\"ReorderLevel\" >= 0");
        });

        builder.Property(p => p.Sku).IsRequired().HasMaxLength(Product.SkuMaxLength).UseCollation("NOCASE");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(Product.NameMaxLength);
        builder.Property(p => p.UnitOfMeasure).IsRequired().HasMaxLength(Product.UnitOfMeasureMaxLength);
        builder.Property(p => p.IsActive).HasDefaultValueSql("1").ValueGeneratedNever();
        builder.HasIndex(p => p.Sku).IsUnique();
    }
}
