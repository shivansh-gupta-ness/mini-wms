using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWms.Domain.Locations;

namespace MiniWms.Infrastructure.Persistence.Configurations;

internal sealed class BinConfiguration : IEntityTypeConfiguration<Bin>
{
    public void Configure(EntityTypeBuilder<Bin> builder)
    {
        builder.ToTable("Bins", t =>
        {
            t.HasCheckConstraint("CK_Bins_Code_Length", $"length(\"Code\") <= {Bin.CodeMaxLength}");
            t.HasCheckConstraint("CK_Bins_Name_Length", $"length(\"Name\") <= {Bin.NameMaxLength}");
        });

        builder.Property(b => b.Code).IsRequired().HasMaxLength(Bin.CodeMaxLength).UseCollation("NOCASE");
        builder.Property(b => b.Name).IsRequired().HasMaxLength(Bin.NameMaxLength);
        builder.Property(b => b.IsActive).HasDefaultValueSql("1").ValueGeneratedNever();
        builder.HasIndex(b => b.Code).IsUnique();

        builder.HasOne<Zone>().WithMany().HasForeignKey(b => b.ZoneId).OnDelete(DeleteBehavior.Restrict);
    }
}
