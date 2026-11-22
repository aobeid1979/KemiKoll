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

    [Fact]
    public async Task GetAsync_ReturnsProductOrNull()
    {
        var product = ChemicalProduct.Create("Acetone", "Exempel Kemi AB", [HazardStatement.Parse("H225")]);
        await _products.AddAsync(product);

        var loaded = await _products.GetAsync(product.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Acetone", loaded.Name);
        Assert.Null(await _products.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_SavesAllChangedValuesIncludingLists()
    {
        var product = ChemicalProduct.Create("Ethanol", "Amfo Company", [HazardStatement.Parse("H225")]);
        product.AddPictogram(Pictogram.FlameOverCircle);
        await _products.AddAsync(product);

        // Edit the way the app does: load, change, save.
        var loaded = (await _products.GetAsync(product.Id))!;
        loaded.Update("Ethanol absolute", "Reagent Chemical Services", [HazardStatement.Parse("H225"), HazardStatement.Parse("H319")], [Pictogram.Flame, Pictogram.ExclamationMark]);
        var saved = await _products.UpdateAsync(loaded);

        var reloaded = (await _products.GetAsync(product.Id))!;
        Assert.True(saved);
        Assert.Equal("Ethanol absolute", reloaded.Name);
        Assert.Equal("Reagent Chemical Services", reloaded.Supplier);
        Assert.Equal(loaded.HazardStatements, reloaded.HazardStatements);
        Assert.Equal(loaded.Pictograms, reloaded.Pictograms);
    }

    [Fact]
    public async Task UpdateAsync_DeletedProduct_ReturnsFalseAndDoesNotRecreateIt()
    {
        var product = ChemicalProduct.Create("Acetone", "Exempel Kemi AB");
        await _products.AddAsync(product);
        await _products.DeleteAsync(product.Id);

        product.Update("Acetone 2", "Exempel Kemi AB", [], []);
        var saved = await _products.UpdateAsync(product);

        Assert.False(saved);
        Assert.Empty(await _products.ListAsync());
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductAndItsDataSheets()
    {
        var product = ChemicalProduct.Create("Acetone", "Exempel Kemi AB");
        var other = ChemicalProduct.Create("Ethanol", "Exempel Kemi AB");
        await _products.AddAsync(product);
        await _products.AddAsync(other);
        await _sheets.AddAsync(SafetyDataSheet.Create(product.Id, new DateOnly(2025, 3, 10), "sv", "a.pdf"));
        await _sheets.AddAsync(SafetyDataSheet.Create(other.Id, new DateOnly(2025, 3, 10), "sv", "b.pdf"));

        await _sheets.DeleteForProductAsync(product.Id);
        var deleted = await _products.DeleteAsync(product.Id);

        Assert.True(deleted);
        Assert.Equal(other.Id, Assert.Single(await _products.ListAsync()).Id);
        Assert.Equal(other.Id, Assert.Single(await _sheets.ListAsync()).ProductId);
        Assert.False(await _products.DeleteAsync(product.Id));
    }

    [Fact]
    public async Task DataSheets_ListForProduct_CorrectAndDeleteOne()
    {
        var product = ChemicalProduct.Create("Ethanol", "Reagent Chemical Services");
        var other = ChemicalProduct.Create("Acetone", "Exempel Kemi AB");
        await _products.AddAsync(product);
        await _products.AddAsync(other);
        var old = SafetyDataSheet.Create(product.Id, new DateOnly(2019, 1, 1), "sv", "old.pdf");
        var latest = SafetyDataSheet.Create(product.Id, new DateOnly(2025, 8, 7), "sv", "new.pdf");
        await _sheets.AddAsync(old);
        await _sheets.AddAsync(latest);
        await _sheets.AddAsync(SafetyDataSheet.Create(other.Id, new DateOnly(2025, 3, 10), "sv", "other.pdf"));

        // Only this product's data sheets.
        Assert.Equal(2, (await _sheets.ListForProductAsync(product.Id)).Count);

        // Correct one, the way the app does: load, change, save.
        var loaded = (await _sheets.GetAsync(latest.Id))!;
        loaded.Correct(new DateOnly(2025, 8, 8), "en");
        Assert.True(await _sheets.UpdateAsync(loaded));
        var reloaded = (await _sheets.GetAsync(latest.Id))!;
        Assert.Equal(new DateOnly(2025, 8, 8), reloaded.RevisionDate);
        Assert.Equal("en", reloaded.LanguageCode);

        // Remove one; the other version stays.
        Assert.True(await _sheets.DeleteAsync(old.Id));
        Assert.Equal(latest.Id, Assert.Single(await _sheets.ListForProductAsync(product.Id)).Id);
        Assert.False(await _sheets.DeleteAsync(old.Id));
        Assert.Null(await _sheets.GetAsync(old.Id));
    }
}
