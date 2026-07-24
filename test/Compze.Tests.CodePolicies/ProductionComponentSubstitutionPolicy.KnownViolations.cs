namespace Compze.Tests.CodePolicies;

public static partial class ProductionComponentSubstitutionPolicy
{
   ///<summary>The acknowledged remaining test doubles for components Compze ships, shrinking to zero:<br/>
   /// fixing one requires deleting its entry here, and nothing new may be added.</summary>
   static class KnownViolations
   {
      ///<summary>Full names of test-declared types standing in for a production component.</summary>
      ///<remarks>The two remaining endpoint registries burn down by discovering through the real
      /// <see cref="Hosting.SameMachine.InterprocessEndpointRegistry"/> the way <see cref="Tessaging.Hosting.Testing.TestingEndpointHost"/>
      /// already does — until then their specifications prove nothing about the announce/discover pipeline a consumer runs. The
      /// navigator burns down by navigating against a real endpoint, which is what gives
      /// <see cref="Tessaging.Typermedia.NavigationSpecification"/> its meaning in the first place.</remarks>
      public static readonly IReadOnlyList<string> TestDoublesForProductionComponents =
      [
         "Compze.Tessaging.Specifications.Typermedia.NavigationSpecification_specification+HandlingNavigator",
         "Compze.Tests.Integration.Hosting.Given_two_best_effort_endpoints+AddressesOfTheHostsEndpoints",
         "Compze.Tests.Integration.Hosting.Given_two_best_effort_endpoints_conversing_in_typermedia+AddressesOfTheHostsEndpoints"
      ];
   }
}
