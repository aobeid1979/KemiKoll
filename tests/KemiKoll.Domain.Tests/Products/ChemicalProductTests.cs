using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;

namespace KemiKoll.Domain.Tests.Products;

public class ChemicalProductTests
{
    private static readonly HazardStatement H225 = HazardStatement.Parse("H225");
    private static readonly HazardStatement H315 = HazardStatement.Parse("H315");

    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var product = ChemicalProduct.Create("  Degreaser X  ", " ACME Kemi AB ");

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Degreaser X", product.Name);
        Assert.Equal("ACME Kemi AB", product.Supplier);
        Assert.Empty(product.HazardStatements);
        Assert.False(product.IsHazardous);
    }

    [Fact]
    public void Create_TwoProducts_GetDifferentIds()
    {
        var first = ChemicalProduct.Create("A", "Supplier");
        var second = ChemicalProduct.Create("A", "Supplier");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_WithHazardStatements_KeepsOrderAndSkipsDuplicates()
    {
        var product = ChemicalProduct.Create("Solvent", "Supplier", [H315, H225, H315]);

        Assert.Equal(new[] { H315, H225 }, product.HazardStatements);
        Assert.True(product.IsHazardous);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => ChemicalProduct.Create(name, "Supplier"));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ChemicalProduct.Create(null!, "Supplier"));
    }

    [Fact]
    public void Create_EmptySupplier_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => ChemicalProduct.Create("Solvent", " "));
        Assert.Equal("supplier", exception.ParamName);
    }

    [Fact]
    public void Create_NameAtMaxLength_IsAllowed()
    {
        var name = new string('a', ChemicalProduct.MaxTextLength);

        var product = ChemicalProduct.Create(name, "Supplier");

        Assert.Equal(name, product.Name);
    }

    [Fact]
    public void Create_NameTooLong_Throws()
    {
        var name = new string('a', ChemicalProduct.MaxTextLength + 1);

        Assert.Throws<ArgumentException>(() => ChemicalProduct.Create(name, "Supplier"));
    }

    [Fact]
    public void Rename_ValidName_ChangesNameAndKeepsId()
    {
        var product = ChemicalProduct.Create("Old name", "Supplier");
        var id = product.Id;

        product.Rename(" New name ");

        Assert.Equal("New name", product.Name);
        Assert.Equal(id, product.Id);
    }

    [Fact]
    public void Rename_EmptyName_ThrowsAndKeepsOldName()
    {
        var product = ChemicalProduct.Create("Old name", "Supplier");

        Assert.Throws<ArgumentException>(() => product.Rename(""));
        Assert.Equal("Old name", product.Name);
    }

    [Fact]
    public void ChangeSupplier_ValidSupplier_ChangesSupplier()
    {
        var product = ChemicalProduct.Create("Solvent", "Old AB");

        product.ChangeSupplier("New AB");

        Assert.Equal("New AB", product.Supplier);
    }

    [Fact]
    public void AddHazardStatement_NewStatement_ReturnsTrue()
    {
        var product = ChemicalProduct.Create("Solvent", "Supplier");

        Assert.True(product.AddHazardStatement(H225));
        Assert.Single(product.HazardStatements);
        Assert.True(product.IsHazardous);
    }

    [Fact]
    public void AddHazardStatement_SameCodeTwice_ReturnsFalseAndKeepsOne()
    {
        var product = ChemicalProduct.Create("Solvent", "Supplier", [H225]);

        // A separately parsed statement with the same code counts as a duplicate.
        var added = product.AddHazardStatement(HazardStatement.Parse("h225"));

        Assert.False(added);
        Assert.Single(product.HazardStatements);
    }

    [Fact]
    public void RemoveHazardStatement_Existing_ReturnsTrue()
    {
        var product = ChemicalProduct.Create("Solvent", "Supplier", [H225]);

        Assert.True(product.RemoveHazardStatement(H225));
        Assert.Empty(product.HazardStatements);
        Assert.False(product.IsHazardous);
    }

    [Fact]
    public void RemoveHazardStatement_Missing_ReturnsFalse()
    {
        var product = ChemicalProduct.Create("Solvent", "Supplier", [H225]);

        Assert.False(product.RemoveHazardStatement(H315));
        Assert.Single(product.HazardStatements);
    }

    [Fact]
    public void AddHazardStatement_Null_Throws()
    {
        var product = ChemicalProduct.Create("Solvent", "Supplier");

        Assert.Throws<ArgumentNullException>(() => product.AddHazardStatement(null!));
    }
}
