using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KemiKoll.Infrastructure.Persistence.Configurations;

/// <summary>
/// How a <see cref="SafetyDataSheet"/> is stored: table <c>safety_data_sheets</c>.
/// </summary>
internal sealed class SafetyDataSheetConfiguration : IEntityTypeConfiguration<SafetyDataSheet>
{
    public void Configure(EntityTypeBuilder<SafetyDataSheet> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("safety_data_sheets");

        builder.HasKey(sheet => sheet.Id);

        builder.Property(sheet => sheet.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(sheet => sheet.ProductId)
            .HasColumnName("product_id");

        // PostgreSQL "date": a calendar date without time, the same as DateOnly.
        builder.Property(sheet => sheet.RevisionDate)
            .HasColumnName("revision_date");

        builder.Property(sheet => sheet.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();

        builder.Property(sheet => sheet.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(SafetyDataSheet.MaxFileNameLength)
            .IsRequired();

        builder.Ignore(sheet => sheet.IsInSwedish);

        // Every data sheet belongs to an existing product. The domain links them by id only,
        // so there is no navigation property; the database still enforces the link.
        // Deleting a product deletes its data sheets.
        builder.HasOne<ChemicalProduct>()
            .WithMany()
            .HasForeignKey(sheet => sheet.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Data sheets are always looked up by product.
        builder.HasIndex(sheet => sheet.ProductId);
    }
}
