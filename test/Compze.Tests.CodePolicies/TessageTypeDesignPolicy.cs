using Compze.Tessaging.Validation;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Sweeps every tessage type the Compze library assemblies declare through the framework's own
/// <see cref="TessageTypeDesignRules"/>. The framework asserts these rules only the first time a type is sent or subscribed to,
/// so a self-contradictory declaration nothing instantiates ships silently — as <c>IStrictlyLocalTommand&lt;TResult&gt;</c> once
/// did: no valid type could implement it under the rules, yet the local typermedia navigators declared doors for exactly that
/// kind. This policy asserts every declared tessage type up front, interfaces and classes alike.</summary>
public static class TessageTypeDesignPolicy
{
   [XF] public static void Every_tessage_type_declared_in_a_Compze_library_assembly_fulfills_the_tessage_type_design_rules()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();
      TessageTypeDesignRules.AssertFulfilledByAllTessageTypesIn(AppDomain.CurrentDomain.AllCompzeLibraryAssemblies());
   }
}
