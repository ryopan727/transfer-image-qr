using System.Reflection;
using TransferImageQR.Application;
using TransferImageQR.Domain;
using TransferImageQR.Infrastructure;
using Xunit;

namespace TransferImageQR.UnitTests;

public sealed class ArchitectureDependencyTests
{
    private const string ProductAssemblyPrefix = "TransferImageQR";

    [Fact]
    public void Domain_DoesNotReferenceOuterProductLayers()
    {
        var references = GetProductReferences(typeof(DomainAssembly).Assembly);

        Assert.Empty(references);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrDesktop()
    {
        var references = GetProductReferences(typeof(ApplicationAssembly).Assembly);

        Assert.DoesNotContain("TransferImageQR.Infrastructure", references);
        Assert.DoesNotContain("TransferImageQR", references);
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceDesktop()
    {
        var references = GetProductReferences(typeof(InfrastructureAssembly).Assembly);

        Assert.DoesNotContain("TransferImageQR", references);
    }

    private static string[] GetProductReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith(ProductAssemblyPrefix, StringComparison.Ordinal))
            .Cast<string>()
            .ToArray();
}
