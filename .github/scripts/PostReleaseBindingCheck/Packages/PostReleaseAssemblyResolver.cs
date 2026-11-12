using Mono.Cecil;

namespace PostReleaseBindingCheck.Packages;

/// <summary>
/// Resolves an assembly reference to the assembly of that name in the <see cref="PostReleasePackageSet"/>, and to nothing else:<br/>
/// a reference to any other assembly, such as the .NET libraries', resolves to <see langword="null"/>, which tells Cecil the<br/>
/// referenced type or member is outside the set, so the check leaves it alone.
/// </summary>
sealed class PostReleaseAssemblyResolver : IAssemblyResolver
{
   IReadOnlyDictionary<string, AssemblyDefinition> _assembliesByName = new Dictionary<string, AssemblyDefinition>();

   /// <summary>Starts resolving to the assemblies of <paramref name="packages"/>, once they are all read.</summary>
   public void ResolveTo(IEnumerable<Package> packages) =>
      _assembliesByName = packages.SelectMany(package => package.Assemblies).ToDictionary(assembly => assembly.Name.Name, StringComparer.OrdinalIgnoreCase);

   public AssemblyDefinition? Resolve(AssemblyNameReference name) => _assembliesByName.GetValueOrDefault(name.Name);

   public AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);

   /// <summary>The <see cref="Package"/>s own the assemblies, and dispose them.</summary>
   public void Dispose() {}
}
