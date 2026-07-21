using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Enforces the namespace-visibility strategy of
/// <c>.claude/rules/02-universal-compze/01-strategy/020-highlight-public-vs-internal-parts-of-projects.md</c>:<br/>
/// a namespace with an <c>_internal</c> or <c>_private</c> section holds no public types, and every top-level internal type
/// lives under such a section.</summary>
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
                                         .Select(type => type.Namespace!)
                                         .Distinct()
                                         .Order(StringComparer.Ordinal)
                                         .ToList();

      violatingNamespaces.Must().SequenceEqual(KnownViolations.NamespacesWithInternalTopLevelTypesOutsideInternalOrPrivateSections);
   }

   ///<summary>True when any section of the namespace is <c>_internal</c> or <c>_private</c> — the sections marking
   /// machinery no library consumer may touch.</summary>
   static bool HasNonPublicSection(string? @namespace) =>
      @namespace?.Split('.').Any(section => section is "_internal" or "_private") == true;
}
