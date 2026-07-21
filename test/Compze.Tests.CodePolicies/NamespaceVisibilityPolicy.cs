using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces the namespace-visibility strategy of
/// <c>.claude/rules/02-universal-compze/01-strategy/020-highlight-public-vs-internal-parts-of-projects.md</c>:<br/>
/// a namespace with an <c>_internal</c> or <c>_private</c> section holds no public types, every top-level internal type
/// lives under such a section, and no namespace resurrects the legacy <c>Public</c> section.</summary>
///<remarks>Each assertion requires the current violations to exactly equal the burn-down list in
/// <c>KnownViolations</c>: fixing a violation forces deleting its entry, and a new violation fails the build.</remarks>
public static partial class NamespaceVisibilityPolicy
{
   [XF] public static void Public_types_never_live_in__internal_or__private_namespaces()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var violations = AppDomain.CurrentDomain.AllCompzeLibraryTypes()
                                .Where(type => type.IsVisible && HasNonPublicSection(type.Namespace))
                                .Select(type => type.FullName!)
                                .Order(StringComparer.Ordinal)
                                .ToList();

      violations.Must().SequenceEqual(KnownViolations.PublicTypesInInternalOrPrivateNamespaces);
   }

   [XF] public static void Internal_top_level_types_always_live_in__internal_or__private_namespaces()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var violatingNamespaces = AppDomain.CurrentDomain.AllCompzeLibraryTypes()
                                         .Where(type => type is { IsNested: false, IsPublic: false }
                                                     && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                                     && type.Namespace?.StartsWithOrdinal("Compze") == true
                                                     && !HasNonPublicSection(type.Namespace))
                                         .Select(type => type.GetFullNameCompilable())
                                         .Distinct()
                                         .Order(StringComparer.Ordinal)
                                         .ToList();

      violatingNamespaces.Must().SequenceEqual(KnownViolations.InternalTopLevelTypesOutsideInternalOrPrivateSections);
   }

   ///<summary>Namespaces named Public were the old strategy's inverse marking — labeling the public face instead of hiding
   /// the non-public one. Public is the default face of a project: what its root namespace shows is expected to be public,
   /// so a section named <c>Public</c> says nothing and may not exist. This polices every Compze assembly, tests included.</summary>
   [XF] public static void No_namespace_contains_a_Public_section()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var violations = AppDomain.CurrentDomain.AllCompzeTypes()
                                .Where(type => type.Namespace?.Split('.').Contains("Public") == true)
                                .Select(type => type.Namespace!)
                                .Distinct()
                                .Order(StringComparer.Ordinal)
                                .ToList();

      violations.Must().SequenceEqual(Array.Empty<string>());
   }

   ///<summary>True when any section of the namespace is <c>_internal</c> or <c>_private</c> — the sections marking
   /// machinery no library consumer may touch.</summary>
   static bool HasNonPublicSection(string? @namespace) =>
      @namespace?.Split('.').Any(section => section is "_internal" or "_private") == true;
}
