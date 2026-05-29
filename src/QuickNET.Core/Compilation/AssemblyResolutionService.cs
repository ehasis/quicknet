using System.Reflection;
using Microsoft.CodeAnalysis;

namespace QuickNET.Compilation;

public class AssemblyResolutionService
{
    private readonly HashSet<string> _loadedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<MetadataReference> _extraReferences = [];

    public IReadOnlyList<MetadataReference> ExtraReferences => _extraReferences.AsReadOnly();
    public IReadOnlyCollection<string> LoadedAssemblyNames => _loadedAssemblyNames;

    public MetadataReference? Resolve(string assemblyName)
    {
        if (_loadedAssemblyNames.Contains(assemblyName))
            return _extraReferences[_loadedAssemblyNames.ToList().IndexOf(assemblyName)];

        try
        {
            var assembly = Assembly.Load(assemblyName);
            if (string.IsNullOrEmpty(assembly.Location))
                return null;

            _loadedAssemblyNames.Add(assemblyName);
            var reference = MetadataReference.CreateFromFile(assembly.Location);
            _extraReferences.Add(reference);
            return reference;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
    }
}
