namespace Compze.Tests.CodePolicies;

public static partial class ProductionComponentSubstitutionPolicy
{
   ///<summary>The acknowledged remaining test doubles for components Compze ships, shrinking to zero:<br/>
   /// fixing one requires deleting its entry here, and nothing new may be added.</summary>
   static class KnownViolations
   {
      ///<summary>Full names of test-declared types standing in for a production component. The list has burned to zero and stays
      /// there: a specification that cannot reach what it needs through the real component has found a design problem to fix or an
      /// internal specification to write, not a component to replace.</summary>
      public static readonly IReadOnlyList<string> TestDoublesForProductionComponents =
      [
      ];
   }
}
