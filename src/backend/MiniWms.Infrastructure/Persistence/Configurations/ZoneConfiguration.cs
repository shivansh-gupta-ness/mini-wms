using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWms.Domain.Locations;

namespace MiniWms.Infrastructure.Persistence.Configurations;

internal sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("Zones", t =>
        {
            t.HasCheckConstraint("CK_Zones_Code_Length", $"length(\"Code\") <= {Zone.CodeMaxLength}");
            t.HasCheckConstraint("CK_Zones_Name_Length", $"length(\"Name\") <= {Zone.NameMaxLength}");
            t.HasCheckConstraint("CK_Zones_Type", $"\"Type\" IN ({string.Join(", ", Enum.GetNames<ZoneType>().Select(n => $"'{n}'"))})");
        });

        builder.Property(z => z.Code).IsRequired().HasMaxLength(Zone.CodeMaxLength).UseCollation("NOCASE");
        builder.Property(z => z.Name).IsRequired().HasMaxLength(Zone.NameMaxLength);
        builder.Property(z => z.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(z => z.IsActive).HasDefaultValueSql("1").ValueGeneratedNever();
        builder.HasIndex(z => z.Code).IsUnique();

        builder.HasOne<Warehouse>().WithMany().HasForeignKey(z => z.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
