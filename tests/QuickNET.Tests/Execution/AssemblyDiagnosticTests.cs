using System.Reflection;

namespace QuickNET.Tests.Execution;

[TestClass]
public class AssemblyDiagnosticTests
{
    [TestMethod]
    public void CheckAssemblyLocations()
    {
        Console.WriteLine($"object: {typeof(object).Assembly.Location}");
        Console.WriteLine($"object: {typeof(object).Assembly.FullName}");

        try
        {
            var sr = Assembly.Load("System.Runtime");
            Console.WriteLine($"System.Runtime: {sr.Location}");
            Console.WriteLine($"System.Runtime: {sr.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"System.Runtime load failed: {ex.Message}");
        }

        Console.WriteLine($"Runtime dir: {Path.GetDirectoryName(typeof(object).Assembly.Location)}");

        var dir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var runtimeDll = Path.Combine(dir, "System.Runtime.dll");
        Console.WriteLine($"System.Runtime.dll exists: {File.Exists(runtimeDll)}");
    }
}
