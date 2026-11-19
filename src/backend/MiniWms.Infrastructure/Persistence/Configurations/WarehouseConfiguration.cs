using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWms.Domain.Locations;

namespace MiniWms.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", t =>
        {
            t.HasCheckConstraint("CK_Warehouses_Code_Length", $"length(\"Code\") <= {Warehouse.CodeMaxLength}");
            t.HasCheckConstraint("CK_Warehouses_Name_Length", $"length(\"Name\") <= {Warehouse.NameMaxLength}");
        });

        builder.Property(w => w.Code).IsRequired().HasMaxLength(Warehouse.CodeMaxLength).UseCollation("NOCASE");
        builder.Property(w => w.Name).IsRequired().HasMaxLength(Warehouse.NameMaxLength);
        builder.Property(w => w.IsActive).HasDefaultValueSql("1").ValueGeneratedNever();
        builder.HasIndex(w => w.Code).IsUnique();
    }
}
