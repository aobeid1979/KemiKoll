using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;
using KemiKoll.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KemiKoll.Infrastructure.Tests.Persistence;

/// <summary>
/// Checks the database mapping without a database: EF Core builds its model and
/// the CREATE TABLE script from the configuration alone, without connecting.
/// </summary>
public sealed class KemiKollDbContextTests : IDisposable
{
    // Never used to connect; only needed so EF Core knows the provider is PostgreSQL.
    private const string UnusedConnectionString = "Host=localhost;Database=kemikoll_model_only";

    private readonly KemiKollDbContext _context = new(
        new DbContextOptionsBuilder<KemiKollDbContext>()
            .UseNpgsql(UnusedConnectionString)
            .Options);

    public void Dispose() => _context.Dispose();

    private IEntityType EntityType<T>() =>
        _context.Model.FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} is not mapped.");

    [Fact]
    public void Products_AreMappedToProductsTableWithExpectedColumns()
    {
        var entity = EntityType<ChemicalProduct>();

        Assert.Equal("products", entity.GetTableName());
        string[] expected = ["hazard_statements", "id", "name", "pictograms", "supplier"];
        Assert.Equal(expected, entity.GetProperties().Select(p => p.GetColumnName()).Order());
    }

    [Fact]
    public void SafetyDataSheets_AreMappedWithCascadingLinkToProducts()
    {
        var entity = EntityType<SafetyDataSheet>();

        Assert.Equal("safety_data_sheets", entity.GetTableName());
        string[] expected = ["file_name", "id", "language_code", "product_id", "revision_date"];
        Assert.Equal(expected, entity.GetProperties().Select(p => p.GetColumnName()).Order());

        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(ChemicalProduct), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void CreateScript_UsesExpectedPostgreSqlTypes()
    {
        var sql = _context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE products", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE safety_data_sheets", sql, StringComparison.Ordinal);
        Assert.Contains("hazard_statements text[] NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("pictograms text[] NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("revision_date date NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("name character varying(200) NOT NULL", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void HazardStatements_AreStoredAsCodesAndReadBack()
    {
        var converter = EntityType<ChemicalProduct>().FindProperty("_hazardStatements")!.GetValueConverter()!;
        List<HazardStatement> statements = [HazardStatement.Parse("H225"), HazardStatement.Parse("H360Fd")];

        var stored = (string[])converter.ConvertToProvider(statements)!;
        var readBack = (List<HazardStatement>)converter.ConvertFromProvider(stored)!;

        string[] expectedCodes = ["H225", "H360Fd"];
        Assert.Equal(expectedCodes, stored);
        Assert.Equal(statements, readBack);
    }

    [Fact]
    public void Pictograms_AreStoredAsCodesAndReadBack()
    {
        var converter = EntityType<ChemicalProduct>().FindProperty("_pictograms")!.GetValueConverter()!;
        List<Pictogram> pictograms = [Pictogram.Flame, Pictogram.Environment];

        var stored = (string[])converter.ConvertToProvider(pictograms)!;
        var readBack = (List<Pictogram>)converter.ConvertFromProvider(stored)!;

        string[] expectedCodes = ["GHS02", "GHS09"];
        Assert.Equal(expectedCodes, stored);
        Assert.Equal(pictograms, readBack);
    }

    [Fact]
    public void ChangeTracking_DetectsHazardStatementAddedToLoadedProduct()
    {
        var product = ChemicalProduct.Create("Acetone", "Supplier", [HazardStatement.Parse("H225")]);
        var entry = _context.Attach(product);
        Assert.Equal(EntityState.Unchanged, entry.State);

        // The list changes in place; EF Core must notice this so the change is saved.
        product.AddHazardStatement(HazardStatement.Parse("H319"));
        _context.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.Property("_hazardStatements").IsModified);
    }

    [Fact]
    public void ChangeTracking_NoChange_StaysUnchanged()
    {
        var product = ChemicalProduct.Create("Acetone", "Supplier", [HazardStatement.Parse("H225")]);
        product.AddPictogram(Pictogram.Flame);
        var entry = _context.Attach(product);

        _context.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Unchanged, entry.State);
    }
}
