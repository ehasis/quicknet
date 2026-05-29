using QuickNET.Compilation;

namespace QuickNET.Tests.Compilation;

[TestClass]
public sealed class AssemblyResolutionServiceTests
{
    private readonly AssemblyResolutionService _resolver;

    public AssemblyResolutionServiceTests()
    {
        _resolver = new AssemblyResolutionService();
    }

    [TestMethod]
    public void Resolve_ValidSystemAssembly_ReturnsMetadataReference()
    {
        var result = _resolver.Resolve("System.Text.Json");

        Assert.IsNotNull(result);
        Assert.IsTrue(_resolver.LoadedAssemblyNames.Contains("System.Text.Json"));
    }

    [TestMethod]
    public void Resolve_ValidCoreLib_ReturnsMetadataReference()
    {
        var result = _resolver.Resolve("System.Runtime");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Resolve_NonExistentAssembly_ReturnsNull()
    {
        var result = _resolver.Resolve("NonExistent.Fake.Assembly.12345");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Resolve_DuplicateCall_UsesCache()
    {
        _resolver.Resolve("System.Text.Json");
        var countBefore = _resolver.ExtraReferences.Count;
        _resolver.Resolve("System.Text.Json");
        var countAfter = _resolver.ExtraReferences.Count;

        Assert.AreEqual(countBefore, countAfter);
    }

    [TestMethod]
    public void ExtraReferences_AfterResolutions_ContainsReferences()
    {
        _resolver.Resolve("System.Text.Json");
        _resolver.Resolve("System.Linq");

        Assert.IsTrue(_resolver.ExtraReferences.Count >= 2);
    }
}
