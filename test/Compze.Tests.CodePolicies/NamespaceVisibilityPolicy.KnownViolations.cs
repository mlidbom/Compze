namespace Compze.Tests.CodePolicies;

public static partial class NamespaceVisibilityPolicy
{
   ///<summary>The acknowledged remaining violations of the namespace-visibility strategy, shrinking to zero:<br/>
   /// fixing a violation requires deleting its entry here, and nothing new may be added.</summary>
   static class KnownViolations
   {
      ///<summary>Full names of publicly visible types living in a namespace with an <c>_internal</c> or <c>_private</c> section —<br/>
      /// each needs a deliberate decision: internalize the type, or move it to a public home.</summary>
      public static readonly IReadOnlyList<string> PublicTypesInInternalOrPrivateNamespaces =
      [
      ];

      ///<summary>Namespaces without an <c>_internal</c> or <c>_private</c> section that hold top-level internal types —<br/>
      /// each burns down by moving its internal types below the concept's <c>_private</c> section — or <c>_internal</c>,
      /// when other assemblies genuinely consume them.</summary>
      public static readonly IReadOnlyList<string> InternalTopLevelTypesOutsideInternalOrPrivateSections =
      [
      ];
   }
}
