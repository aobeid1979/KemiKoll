using System.Reflection;

namespace KemiKoll.Application.Tests;

/// <summary>
/// Guards the Clean Architecture dependency rule for the Application layer.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        // Loaded by name because the Application project has no types yet.
        var application = Assembly.Load("KemiKoll.Application");

        var kemiKollReferences = application
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("KemiKoll.", StringComparison.Ordinal));

        // Application may use Domain, but never Infrastructure or Web.
        // (The compiler only records references that are actually used,
        // so Domain may not appear in the list until Application uses it.)
        Assert.All(kemiKollReferences, name => Assert.Equal("KemiKoll.Domain", name));
    }
}
