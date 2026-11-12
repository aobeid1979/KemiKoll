using System.Reflection;

namespace KemiKoll.Domain.Tests;

/// <summary>
/// Guards the Clean Architecture dependency rule for the Domain layer.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotDependOnOtherKemiKollProjects()
    {
        // Loaded by name because the Domain project has no types yet.
        var domain = Assembly.Load("KemiKoll.Domain");

        var kemiKollReferences = domain
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("KemiKoll.", StringComparison.Ordinal));

        // Domain is the core of the application: it must not know about any other layer.
        Assert.Empty(kemiKollReferences);
    }
}
