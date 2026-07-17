using Compze.Abstractions.Hosting.Public;
using Compze.Must;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Storage;

///<summary>An exactly-once endpoint's tables live in the domain database it joins under the endpoint's name as prefix<br/>
/// (<see cref="EndpointTableSet"/>) — which is what makes the name identifier material: a letter followed by letters, digits,<br/>
/// or underscores, within the length cap. Asserted loud at composition, never sanitized silently.</summary>
public class EndpointTableSet_specification
{
   static EndpointTableSet TableSetForEndpointNamed(string name) =>
      EndpointTableSet.For(new EndpointConfiguration(name, new EndpointId(Guid.NewGuid())));

   [XF] public void the_tables_are_prefixed_with_the_endpoints_name()
   {
      var tables = TableSetForEndpointNamed("Backend");
      tables.InboxTessages.Must().Be("Backend_InboxTessages");
      tables.OutboxTessages.Must().Be("Backend_OutboxTessages");
      tables.OutboxTessageDispatching.Must().Be("Backend_OutboxTessageDispatching");
      tables.Peers.Must().Be("Backend_Peers");
      tables.PeerHandledTessageTypes.Must().Be("Backend_PeerHandledTessageTypes");
   }

   [XF] public void a_name_containing_a_dot_fails_loud_naming_the_name_and_the_identifier_material_rule() =>
      Invoking(() => TableSetForEndpointNamed("AccountManagement.Statistics"))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("AccountManagement.Statistics")
         .Contain("identifier material");

   [XF] public void a_name_starting_with_a_digit_fails_loud_naming_the_identifier_material_rule() =>
      Invoking(() => TableSetForEndpointNamed("1stBackend"))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("identifier material");

   [XF] public void a_name_longer_than_the_cap_fails_loud_naming_the_cap() =>
      Invoking(() => TableSetForEndpointNamed(new string('A', EndpointTableSet.MaximumEndpointNameLength + 1)))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain($"{EndpointTableSet.MaximumEndpointNameLength}");

   [XF] public void a_name_exactly_at_the_cap_is_accepted() =>
      TableSetForEndpointNamed(new string('A', EndpointTableSet.MaximumEndpointNameLength))
         .InboxTessages.Must().Contain("_InboxTessages");
}
