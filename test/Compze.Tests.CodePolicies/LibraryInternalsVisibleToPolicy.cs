using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces what lets a Compze package require an exact version of each Compze package whose internals it uses
/// (<c>dev_docs/WIP/exact-versions-on-internals.md</c>): a library that another library grants
/// <see cref="InternalsVisibleToAttribute"/> references that library directly.</summary>
///<remarks>Internals carry no compatibility promise, so a package that uses another's internals requires the exact version it was
/// built against. Packing writes that exact version only for a library the package references directly, since NuGet writes dependencies
/// only for those. A library that reached the granting library only through another would require no more than the minimum version the
/// other requires, and would restore beside any newer build of the internals it uses.</remarks>
public static class LibraryInternalsVisibleToPolicy
{
   [XF] public static void A_library_that_another_library_grants_its_internals_references_that_library_directly()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();
      var libraries = CompzeRepository.LibraryProjectNames.ToHashSet(StringComparer.Ordinal);

      var violations = AppDomain.CurrentDomain.AllCompzeAssemblies()
                                .Where(grantingLibrary => libraries.Contains(grantingLibrary.GetName().Name!))
                                .SelectMany(grantingLibrary => LibrariesGrantedTheInternalsOf(grantingLibrary, libraries)
                                                                  .Where(grantee => !DirectDependencies.Of(grantee).Contains(grantingLibrary.GetName().Name!))
                                                                  .Select(grantee => $"{grantingLibrary.GetName().Name} grants {grantee} its internals, but {grantee} doesn't reference {grantingLibrary.GetName().Name} directly"))
                                .Distinct()
                                .Order(StringComparer.Ordinal)
                                .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   static IEnumerable<string> LibrariesGrantedTheInternalsOf(Assembly grantingLibrary, IReadOnlySet<string> libraries) =>
      grantingLibrary.GetCustomAttributes<InternalsVisibleToAttribute>()
                     .Select(grant => grant.AssemblyName.Split(',')[0].Trim())
                     .Where(libraries.Contains);
}
