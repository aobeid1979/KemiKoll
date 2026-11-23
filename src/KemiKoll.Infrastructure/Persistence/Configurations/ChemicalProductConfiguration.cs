using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KemiKoll.Infrastructure.Persistence.Configurations;

/// <summary>
/// How a <see cref="ChemicalProduct"/> is stored: table <c>products</c>.
/// </summary>
/// <remarks>
/// Hazard statements and pictograms are stored as PostgreSQL text arrays of their codes,
/// for example <c>{H225,H319}</c> and <c>{GHS02,GHS07}</c>. They are small, always loaded
/// together with the product, and readable directly in the database.
/// </remarks>
internal sealed class ChemicalProductConfiguration : IEntityTypeConfiguration<ChemicalProduct>
{
    /// <summary>Name of the private field in <see cref="ChemicalProduct"/> that holds the hazard statements.</summary>
    internal const string HazardStatementsField = "_hazardStatements";

    /// <summary>Name of the private field in <see cref="ChemicalProduct"/> that holds the pictograms.</summary>
    internal const string PictogramsField = "_pictograms";

    public void Configure(EntityTypeBuilder<ChemicalProduct> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        // The id is created by the domain (Guid version 7), never by the database.
        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(ChemicalProduct.MaxTextLength)
            .IsRequired();

        builder.Property(product => product.Supplier)
            .HasColumnName("supplier")
            .HasMaxLength(ChemicalProduct.MaxTextLength)
            .IsRequired();

        // timestamp with time zone; the domain always stores UTC, as Npgsql requires.
        builder.Property(product => product.AddedAt)
            .HasColumnName("added_at");

        builder.Property(product => product.LastChangedAt)
            .HasColumnName("last_changed_at");

        // The public lists are read-only views of private fields; EF Core works with the fields.
        builder.Ignore(product => product.HazardStatements);
        builder.Ignore(product => product.Pictograms);
        builder.Ignore(product => product.IsHazardous);

        builder.Property<List<HazardStatement>>(HazardStatementsField)
            .HasField(HazardStatementsField)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("hazard_statements")
            .HasConversion(HazardStatementsConverter, ListComparer<HazardStatement>())
            .IsRequired();

        builder.Property<List<Pictogram>>(PictogramsField)
            .HasField(PictogramsField)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("pictograms")
            .HasConversion(PictogramsConverter, ListComparer<Pictogram>())
            .IsRequired();
    }

    // List<HazardStatement> ⇄ text[] of codes. Parse cannot fail on data written by this converter.
    private static readonly ValueConverter<List<HazardStatement>, string[]> HazardStatementsConverter = new(
        statements => statements.Select(statement => statement.Code).ToArray(),
        codes => codes.Select(code => HazardStatement.Parse(code)).ToList());

    // List<Pictogram> ⇄ text[] of codes such as "GHS02" (not numbers), so the data is readable in the database.
    private static readonly ValueConverter<List<Pictogram>, string[]> PictogramsConverter = new(
        pictograms => pictograms.Select(pictogram => pictogram.ToCode()).ToArray(),
        codes => codes.Select(code => PictogramCodes.Parse(code)).ToList());

    // Lists can change in place (Add/Remove), so EF Core needs to compare their contents
    // and keep a copy (snapshot) to detect changes. The default would only compare references.
    private static ValueComparer<List<T>> ListComparer<T>() => new(
        (left, right) => left!.SequenceEqual(right!),
        list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item!.GetHashCode())),
        list => list.ToList());
}
