using Compze.Tessaging.Endpoints;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>An exactly-once endpoint's name is identifier material — it prefixes the endpoint's table-set in the domain<br/>
/// database it joins: a letter followed by letters, digits, or underscores, at most 28 characters (PostgreSQL's identifier<br/>
/// limit minus the longest generated identifier). Asserted loud at composition, never sanitized silently.</summary>
public class Given_an_exactly_once_endpoint_composition : UniversalTestBase
{
   [PCT] public async Task whose_name_contains_a_dot_composition_fails_loud_naming_the_name_and_the_identifier_material_rule()
   {
      await using var host = TestingEndpointHost.Create();
      Invoking(() => RegisterEndpointNamed(host, "AccountManagement.Statistics"))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("AccountManagement.Statistics")
         .Contain("identifier material");
   }

   [PCT] public async Task whose_name_starts_with_a_digit_composition_fails_loud_naming_the_identifier_material_rule()
   {
      await using var host = TestingEndpointHost.Create();
      Invoking(() => RegisterEndpointNamed(host, "1stBackend"))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("identifier material");
   }

   [PCT] public async Task whose_name_exceeds_the_28_character_cap_composition_fails_loud_naming_the_cap()
   {
      await using var host = TestingEndpointHost.Create();
      Invoking(() => RegisterEndpointNamed(host, new string('A', 29)))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("28");
   }

   [PCT] public async Task whose_name_is_exactly_at_the_28_character_cap_composes()
   {
      await using var host = TestingEndpointHost.Create();
      RegisterEndpointNamed(host, new string('A', 28)).Must().NotBeNull();
   }

   static ExactlyOnceEndpoint RegisterEndpointNamed(TestingEndpointHost host, string name) =>
      host.RegisterExactlyOnceEndpoint(
         name,
         new EndpointId(Guid.NewGuid()),
         endpointBuilder => endpointBuilder.RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings()));
}
