using KemiKoll.Application.Documents;
using KemiKoll.Application.Products;
using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Infrastructure.Documents;
using KemiKoll.Infrastructure.Products;
using KemiKoll.Infrastructure.SafetyDataSheets;

using Microsoft.Extensions.DependencyInjection;

namespace KemiKoll.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInfrastructure_WithoutConnectionString_UsesInMemoryStorage(string? connectionString)
    {
        using var provider = new ServiceCollection().AddInfrastructure(connectionString).BuildServiceProvider();

        Assert.IsType<InMemoryProductRepository>(provider.GetRequiredService<IProductRepository>());
        Assert.IsType<InMemorySafetyDataSheetRepository>(provider.GetRequiredService<ISafetyDataSheetRepository>());
    }

    [Fact]
    public void AddInfrastructure_WithConnectionString_UsesPostgreSql()
    {
        // Resolving the repositories does not open a connection, so no database is needed here.
        using var provider = new ServiceCollection()
            .AddInfrastructure("Host=localhost;Database=kemikoll;Username=kemikoll;Password=unused")
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<EfProductRepository>(scope.ServiceProvider.GetRequiredService<IProductRepository>());
        Assert.IsType<EfSafetyDataSheetRepository>(scope.ServiceProvider.GetRequiredService<ISafetyDataSheetRepository>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Host=localhost;Database=kemikoll;Username=kemikoll;Password=unused")]
    public void AddInfrastructure_AlwaysRegistersPdfReader(string? connectionString)
    {
        using var provider = new ServiceCollection().AddInfrastructure(connectionString).BuildServiceProvider();

        Assert.IsType<PdfPigTextReader>(provider.GetRequiredService<IPdfTextReader>());
    }
}
