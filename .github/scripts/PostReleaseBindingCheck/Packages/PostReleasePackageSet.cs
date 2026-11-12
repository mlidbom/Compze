using Mono.Cecil;
using PostReleaseBindingCheck.Checks;
using PostReleaseBindingCheck.Failures;

namespace PostReleaseBindingCheck.Packages;

/// <summary>
/// The Compze packages nuget.org serves once a release is out: for each package id, the newest version a consumer gets.<br/>
/// That is the release's own build of each package the release publishes, and nuget.org's newest version of every other one.
/// </summary>
/// <remarks>
/// A consumer that upgrades the packages a release publishes runs them together with the other packages exactly as nuget.org<br/>
/// has them, and nothing in Compze's own build ever runs that mix: the solution builds every package against the source of<br/>
/// the others. So a package that changes in a release can break a released package that uses it, or rely on a change in<br/>
/// another package that was never released, and only a consumer finds out, at runtime, with a <see cref="TypeLoadException"/>,<br/>
/// <see cref="MissingMethodException"/> or <see cref="MemberAccessException"/>.<br/>
/// <see cref="FindBindingFailures"/> finds those before the release publishes anything.
/// </remarks>
public sealed class PostReleasePackageSet : IDisposable
{
   readonly PostReleaseAssemblyResolver _assemblyResolver;
   readonly IReadOnlyDictionary<string, Package> _packagesByAssemblyName;

   PostReleasePackageSet(IReadOnlyList<Package> packages, PostReleaseAssemblyResolver assemblyResolver)
   {
      var duplicateIds = packages.GroupBy(package => package.Id, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
      Argument.Assert(duplicateIds.Count == 0, () => $"Each package must be given once, but these were given more than once: {string.Join(", ", duplicateIds)}");

      Packages = packages;
      _assemblyResolver = assemblyResolver;
      _assemblyResolver.ResolveTo(packages);
      _packagesByAssemblyName = packages.SelectMany(package => package.Assemblies.Select(assembly => (assembly, package)))
                                        .ToDictionary(it => it.assembly.Name.Name, it => it.package, StringComparer.OrdinalIgnoreCase);
   }

   public IReadOnlyList<Package> Packages { get; }

   /// <summary>
   /// Reads the set from the nupkg files of the packages the release publishes, <paramref name="thisReleaseNupkgs"/>,<br/>
   /// and those of nuget.org's newest version of every other Compze package, <paramref name="nuGetOrgNupkgs"/>.
   /// </summary>
   public static PostReleasePackageSet Load(IEnumerable<string> thisReleaseNupkgs, IEnumerable<string> nuGetOrgNupkgs)
   {
      var assemblyResolver = new PostReleaseAssemblyResolver();
      var packages = thisReleaseNupkgs.Select(nupkg => Package.ReadNupkg(nupkg, PackageOrigin.ThisRelease, assemblyResolver))
                                      .Concat(nuGetOrgNupkgs.Select(nupkg => Package.ReadNupkg(nupkg, PackageOrigin.NuGetOrg, assemblyResolver)))
                                      .ToList();
      return new PostReleasePackageSet(packages, assemblyResolver);
   }

   /// <summary>
   /// Every way a package of the set would fail at runtime against another package of it: each reference into another package<br/>
   /// that does not resolve there (<see cref="ReferenceCheck"/>), and each type the runtime would refuse to load because of what<br/>
   /// another package's base class or interface has become (<see cref="TypeLoadCheck"/>).
   /// </summary>
   public IReadOnlyList<BindingFailure> FindBindingFailures() =>
   [
      ..Packages.SelectMany(package => package.Assemblies.SelectMany(assembly => new ReferenceCheck(this, package, assembly).FindFailures()
                                                                                    .Concat(new TypeLoadCheck(this, package, assembly).FindFailures())))
   ];

   /// <summary>
   /// The package of the set whose assembly a reference such as <paramref name="type"/> points into,<br/>
   /// or <see langword="null"/> when it points outside the set, such as into the .NET libraries.
   /// </summary>
   internal Package? PackageReferencedBy(TypeReference type) =>
      type.GetElementType().Scope is AssemblyNameReference referencedAssembly
         ? _packagesByAssemblyName.GetValueOrDefault(referencedAssembly.Name)
         : null;

   /// <summary>The package of the set that holds <paramref name="type"/>, a type Cecil resolved within the set.</summary>
   internal Package PackageContaining(TypeDefinition type) => _packagesByAssemblyName[type.Module.Assembly.Name.Name];

   public void Dispose()
   {
      foreach(var package in Packages) package.Dispose();
      _assemblyResolver.Dispose();
   }
}
