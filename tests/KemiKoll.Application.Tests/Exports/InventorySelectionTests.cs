using KemiKoll.Application.Exports;
using KemiKoll.Application.Products;

namespace KemiKoll.Application.Tests.Exports;

public class InventorySelectionTests
{
    [Fact]
    public void Everything_HasNoDescription()
    {
        Assert.True(InventorySelection.Everything.IsEverything);
        Assert.Null(InventorySelection.Everything.Describe());
    }

    [Theory]
    [InlineData(null, ProductFilter.NeedsAttention, "Needs attention")]
    [InlineData("  h225 ", ProductFilter.All, "Search: h225")]
    [InlineData("aspen", ProductFilter.NeedsAttention, "Needs attention · Search: aspen")]
    public void Describe_NamesFilterAndSearch(string? text, ProductFilter filter, string expected)
    {
        var selection = new InventorySelection(text, filter);

        Assert.False(selection.IsEverything);
        Assert.Equal(expected, selection.Describe());
    }
}
