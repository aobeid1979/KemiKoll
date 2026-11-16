using KemiKoll.Domain.Companies;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Domain.Tests.Companies;

public class CompanyTests
{
    private static readonly OrganizationNumber OrgNumber = OrganizationNumber.Parse("556036-0793");

    private static Company CreateCompany() => Company.Create("Bilverkstad AB", OrgNumber);

    [Fact]
    public void Create_ValidInput_SetsPropertiesAndDefaults()
    {
        var company = Company.Create("  Bilverkstad AB  ", OrgNumber);

        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("Bilverkstad AB", company.Name);
        Assert.Equal(OrgNumber, company.OrganizationNumber);
        Assert.Equal(ReviewPolicy.Default, company.ReviewPolicy);
        Assert.Empty(company.Workplaces);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => Company.Create(" ", OrgNumber));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_NullOrganizationNumber_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Company.Create("Bilverkstad AB", null!));
    }

    [Fact]
    public void Create_NameTooLong_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Company.Create(new string('a', Company.MaxNameLength + 1), OrgNumber));
    }

    [Fact]
    public void Rename_ValidName_ChangesName()
    {
        var company = CreateCompany();

        company.Rename("CNC Verkstad AB");

        Assert.Equal("CNC Verkstad AB", company.Name);
    }

    [Fact]
    public void ChangeReviewPolicy_SetsCompanyChoice()
    {
        var company = CreateCompany();

        company.ChangeReviewPolicy(ReviewPolicy.Create(5));

        Assert.Equal(5, company.ReviewPolicy.MaxAgeInYears);
    }

    [Fact]
    public void ChangeReviewPolicy_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreateCompany().ChangeReviewPolicy(null!));
    }

    [Fact]
    public void AddWorkplace_ValidName_AddsAndReturnsWorkplace()
    {
        var company = CreateCompany();

        var workplace = company.AddWorkplace("  Workshop  ");

        Assert.NotEqual(Guid.Empty, workplace.Id);
        Assert.Equal("Workshop", workplace.Name);
        Assert.Equal(new[] { workplace }, company.Workplaces);
    }

    [Fact]
    public void AddWorkplace_KeepsOrder()
    {
        var company = CreateCompany();

        company.AddWorkplace("Workshop");
        company.AddWorkplace("Chemical cabinet 1");

        string[] expected = ["Workshop", "Chemical cabinet 1"];
        Assert.Equal(expected, company.Workplaces.Select(w => w.Name));
    }

    [Theory]
    [InlineData("Workshop")]
    [InlineData("workshop")]
    [InlineData(" WORKSHOP ")]
    public void AddWorkplace_DuplicateName_Throws(string duplicate)
    {
        var company = CreateCompany();
        company.AddWorkplace("Workshop");

        Assert.Throws<ArgumentException>(() => company.AddWorkplace(duplicate));
        Assert.Single(company.Workplaces);
    }

    [Fact]
    public void AddWorkplace_NameTooLong_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CreateCompany().AddWorkplace(new string('a', Workplace.MaxNameLength + 1)));
    }

    [Fact]
    public void RenameWorkplace_ValidName_ChangesNameAndKeepsId()
    {
        var company = CreateCompany();
        var workplace = company.AddWorkplace("Workshop");

        company.RenameWorkplace(workplace.Id, "Paint booth");

        Assert.Equal("Paint booth", workplace.Name);
    }

    [Fact]
    public void RenameWorkplace_ToSameNameWithOtherCase_IsAllowed()
    {
        var company = CreateCompany();
        var workplace = company.AddWorkplace("workshop");

        company.RenameWorkplace(workplace.Id, "Workshop");

        Assert.Equal("Workshop", workplace.Name);
    }

    [Fact]
    public void RenameWorkplace_ToNameOfAnotherWorkplace_ThrowsAndKeepsOldName()
    {
        var company = CreateCompany();
        company.AddWorkplace("Workshop");
        var cabinet = company.AddWorkplace("Cabinet");

        Assert.Throws<ArgumentException>(() => company.RenameWorkplace(cabinet.Id, "workshop"));
        Assert.Equal("Cabinet", cabinet.Name);
    }

    [Fact]
    public void RenameWorkplace_UnknownId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => CreateCompany().RenameWorkplace(Guid.NewGuid(), "Workshop"));
    }

    [Fact]
    public void RemoveWorkplace_Existing_ReturnsTrue()
    {
        var company = CreateCompany();
        var workplace = company.AddWorkplace("Workshop");

        Assert.True(company.RemoveWorkplace(workplace.Id));
        Assert.Empty(company.Workplaces);
    }

    [Fact]
    public void RemoveWorkplace_Unknown_ReturnsFalse()
    {
        Assert.False(CreateCompany().RemoveWorkplace(Guid.NewGuid()));
    }
}
