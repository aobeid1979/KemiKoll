using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;

using Microsoft.EntityFrameworkCore;

namespace KemiKoll.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context: the connection between the domain model and the PostgreSQL tables.
/// </summary>
/// <remarks>
/// The domain classes contain no database code. How each class is stored is described
/// in the configuration classes in the <c>Configurations</c> folder, which are found automatically.
/// </remarks>
/// <param name="options">Database provider and connection string, set up in <c>Program.cs</c>.</param>
public sealed class KemiKollDbContext(DbContextOptions<KemiKollDbContext> options) : DbContext(options)
{
    /// <summary>The chemical products (table <c>products</c>).</summary>
    public DbSet<ChemicalProduct> Products => Set<ChemicalProduct>();

    /// <summary>The safety data sheets (table <c>safety_data_sheets</c>).</summary>
    public DbSet<SafetyDataSheet> SafetyDataSheets => Set<SafetyDataSheet>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Applies every IEntityTypeConfiguration in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KemiKollDbContext).Assembly);
    }
}
