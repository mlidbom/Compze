namespace Compze.Tests.CodePolicies;

public static partial class NamespaceVisibilityPolicy
{
   ///<summary>The acknowledged remaining violations of the namespace-visibility strategy, shrinking to zero:<br/>
   /// fixing a violation requires deleting its entry here, and nothing new may be added.</summary>
   static class KnownViolations
   {
      ///<summary>Full names of publicly visible types living in a namespace with an Internal or Private section —<br/>
      /// each needs a deliberate decision: internalize the type, or move it to a public home.</summary>
      public static readonly IReadOnlyList<string> PublicTypesInInternalOrPrivateNamespaces =
      [
      ];

      ///<summary>Namespaces without an Internal or Private section that hold top-level internal types —<br/>
      /// each burns down by moving its internal types below the concept's Internal namespace.</summary>
      public static readonly IReadOnlyList<string> NamespacesWithInternalTopLevelTypesOutsideInternalOrPrivateSections =
      [
      ];
   }
}
