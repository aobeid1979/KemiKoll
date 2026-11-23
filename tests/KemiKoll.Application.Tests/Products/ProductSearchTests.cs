using KemiKoll.Application.Products;

namespace KemiKoll.Application.Tests.Products;

public class ProductSearchTests
{
    private static ProductSummary Product(string name, string supplier, params string[] hazardCodes) =>
        new(Guid.NewGuid(), name, supplier, hazardCodes, ["GHS02"], hazardCodes.Length > 0,
            DataSheetStatus.Missing, null, [], []);

    private static readonly ProductSummary Bensin = Product("Aspen 4T Bensin", "Lantmännen Aspen AB", "H224", "H304");
    private static readonly ProductSummary Degreaser = Product("Avfettning Plus", "Kemetyl AB", "H315");
    private static readonly ProductSummary Oil = Product("Skärolja", "Ståhl AB");
    private static readonly IReadOnlyList<ProductSummary> All = [Bensin, Degreaser, Oil];

    // Checks that the search text finds exactly these products, in list order.
    private static void AssertFinds(string? text, params ProductSummary[] expected) =>
        Assert.Equal(expected, ProductSearch.Filter(All, text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Filter_EmptyText_ReturnsAll(string? text) => AssertFinds(text, Bensin, Degreaser, Oil);

    [Fact]
    public void Filter_PartOfName_IgnoresCase() => AssertFinds("avfett", Degreaser);

    [Fact]
    public void Filter_SwedishLetters_IgnoresCase() => AssertFinds("SKÄR", Oil);

    [Fact]
    public void Filter_Supplier() => AssertFinds("kemetyl", Degreaser);

    [Fact]
    public void Filter_HazardCode() => AssertFinds("h315", Degreaser);

    [Fact]
    public void Filter_PictogramCode() => AssertFinds("ghs02", Bensin, Degreaser, Oil);

    [Fact]
    public void Filter_SeveralWords_AllMustMatch()
    {
        AssertFinds("aspen h304", Bensin);
        AssertFinds("aspen h315");
    }

    [Fact]
    public void Filter_NoMatch_ReturnsEmpty() => AssertFinds("aceton");
}
