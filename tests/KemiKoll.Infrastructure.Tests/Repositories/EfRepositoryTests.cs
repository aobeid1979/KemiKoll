using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;
using KemiKoll.Infrastructure.Products;
using KemiKoll.Infrastructure.SafetyDataSheets;

using Microsoft.EntityFrameworkCore;

namespace KemiKoll.Infrastructure.Tests.Repositories;

/// <summary>
/// Saves and loads through the real repositories and a real PostgreSQL database.
/// </summary>
[Collection(PostgreSqlTestGroup.Name)]
public sealed class EfRepositoryTests(PostgreSqlFixture database) : IAsyncLifetime
{
    private readonly EfProductRepository _products = new(database.ContextFactory);
    private readonly EfSafetyDataSheetRepository _sheets = new(database.ContextFactory);

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Product_IsSavedAndLoadedWithAllValues()
    {
        var product = ChemicalProduct.Create(
            "Brake cleaner",
            "Verkstadsprodukter AB",
            [HazardStatement.Parse("H225"), HazardStatement.Parse("H360Fd"), HazardStatement.Parse("EUH066")]);
        product.AddPictogram(Pictogram.Flame);
        product.AddPictogram(Pictogram.HealthHazard);

        await _products.AddAsync(product);
        var loaded = Assert.Single(await _products.ListAsync());

        Assert.Equal(product.Id, loaded.Id);
        Assert.Equal("Brake cleaner", loaded.Name);
        Assert.Equal("Verkstadsprodukter AB", loaded.Supplier);
        Assert.Equal(product.HazardStatements, loaded.HazardStatements);
        Assert.Equal(product.Pictograms, loaded.Pictograms);
        Assert.True(loaded.IsHazardous);
    }

    [Fact]
    public async Task ProductWithoutHazards_IsSavedAndLoaded()
    {
        await _products.AddAsync(ChemicalProduct.Create("Hand cleaner", "Exempel Kemi AB"));

        var loaded = Assert.Single(await _products.ListAsync());

        Assert.Empty(loaded.HazardStatements);
        Assert.Empty(loaded.Pictograms);
        Assert.False(loaded.IsHazardous);
    }

    [Fact]
    public async Task SafetyDataSheet_IsSavedAndLoadedWithAllValues()
    {
        var product = ChemicalProduct.Create("Acetone", "Exempel Kemi AB");
        await _products.AddAsync(product);
        var sheet = SafetyDataSheet.Create(product.Id, new DateOnly(2025, 3, 10), "sv", "acetone-sds.pdf");

        await _sheets.AddAsync(sheet);
        var loaded = Assert.Single(await _sheets.ListAsync());

        Assert.Equal(sheet.Id, loaded.Id);
        Assert.Equal(product.Id, loaded.ProductId);
        Assert.Equal(new DateOnly(2025, 3, 10), loaded.RevisionDate);
        Assert.Equal("sv", loaded.LanguageCode);
        Assert.Equal("acetone-sds.pdf", loaded.FileName);
    }

    [Fact]
    public async Task SafetyDataSheet_ForUnknownProduct_IsRejectedByTheDatabase()
    {
        var sheet = SafetyDataSheet.Create(Guid.NewGuid(), new DateOnly(2025, 3, 10), "sv", "orphan.pdf");

        await Assert.ThrowsAsync<DbUpdateException>(() => _sheets.AddAsync(sheet));
        Assert.Empty(await _sheets.ListAsync());
    }

    [Fact]
    public async Task DeletingProduct_DeletesItsDataSheets()
    {
        var product = ChemicalProduct.Create("Acetone", "Exempel Kemi AB");
        await _products.AddAsync(product);
        await _sheets.AddAsync(SafetyDataSheet.Create(product.Id, new DateOnly(2025, 3, 10), "sv", "a.pdf"));

        await using (var context = database.CreateContext())
        {
            await context.Products.Where(p => p.Id == product.Id).ExecuteDeleteAsync();
        }

        Assert.Empty(await _sheets.ListAsync());
    }
}
