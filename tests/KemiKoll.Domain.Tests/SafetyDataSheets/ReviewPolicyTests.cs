using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Domain.Tests.SafetyDataSheets;

public class ReviewPolicyTests
{
    [Fact]
    public void Default_IsThreeYears()
    {
        Assert.Equal(3, ReviewPolicy.Default.MaxAgeInYears);
    }

    [Theory]
    [InlineData(ReviewPolicy.MinYears)]
    [InlineData(5)]
    [InlineData(ReviewPolicy.MaxYears)]
    public void Create_YearsInRange_SetsMaxAge(int years)
    {
        Assert.Equal(years, ReviewPolicy.Create(years).MaxAgeInYears);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_YearsOutOfRange_Throws(int years)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReviewPolicy.Create(years));
    }

    [Fact]
    public void Policies_WithSameYears_AreEqual()
    {
        Assert.Equal(ReviewPolicy.Default, ReviewPolicy.Create(3));
    }
}
