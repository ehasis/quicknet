using System.Reflection;
using System.Runtime.Loader;

namespace QuickNET.Execution;

public class QuickNETAssemblyLoadContext : AssemblyLoadContext
{
    public QuickNETAssemblyLoadContext()
        : base(isCollectible: true)
    {
    }

    public Assembly LoadFromBytes(byte[] assemblyBytes)
    {
        using var ms = new MemoryStream(assemblyBytes);
        return LoadFromStream(ms);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        return null;
    }
}
