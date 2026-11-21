using KemiKoll.Application.Documents;
using KemiKoll.Application.Products;
using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Infrastructure.Documents;
using KemiKoll.Infrastructure.Persistence;
using KemiKoll.Infrastructure.Products;
using KemiKoll.Infrastructure.SafetyDataSheets;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KemiKoll.Infrastructure;

/// <summary>
/// Registers the Infrastructure services (storage) in the application's service container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the PDF reader and the repositories: PostgreSQL when a connection string
    /// is given, otherwise the in-memory versions.
    /// </summary>
    /// <param name="services">The service collection of the application.</param>
    /// <param name="connectionString">
    /// The PostgreSQL connection string, or <see langword="null"/>/empty to keep data in memory.
    /// In memory, all data is lost when the application stops.
    /// </param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Reading PDF files does not depend on the storage choice. No state, so one shared instance.
        services.AddSingleton<IPdfTextReader, PdfPigTextReader>();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // One shared instance each, so all users see the same data while the app runs.
            services.AddSingleton<IProductRepository, InMemoryProductRepository>();
            services.AddSingleton<ISafetyDataSheetRepository, InMemorySafetyDataSheetRepository>();
            return services;
        }

        // A factory instead of one shared context: each repository call gets its own context.
        services.AddDbContextFactory<KemiKollDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<ISafetyDataSheetRepository, EfSafetyDataSheetRepository>();
        return services;
    }
}
