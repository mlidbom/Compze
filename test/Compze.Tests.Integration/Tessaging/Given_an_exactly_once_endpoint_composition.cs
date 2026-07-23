using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection.Abstractions;
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
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationWhoseNameContainsADot()))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain(EndpointDeclarationWhoseNameContainsADot.Name)
         .Contain("identifier material");
   }

   [PCT] public async Task whose_name_starts_with_a_digit_composition_fails_loud_naming_the_identifier_material_rule()
   {
      await using var host = TestingEndpointHost.Create();
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationWhoseNameStartsWithADigit()))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("identifier material");
   }

   [PCT] public async Task whose_name_exceeds_the_28_character_cap_composition_fails_loud_naming_the_cap()
   {
      await using var host = TestingEndpointHost.Create();
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationWhoseNameExceedsTheCap()))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain("28");
   }

   [PCT] public async Task whose_name_is_exactly_at_the_28_character_cap_composes()
   {
      await using var host = TestingEndpointHost.Create();
      host.RegisterEndpoint(new EndpointDeclarationWhoseNameIsExactlyAtTheCap()).Must().NotBeNull();
   }

   class EndpointDeclarationWhoseNameContainsADot : ExactlyOnceEndpointDeclaration<EndpointDeclarationWhoseNameContainsADot>, IEndpointIdentity
   {
      public static string Name => "AccountManagement.Statistics";
      public static EndpointId Id { get; } = new(Guid.Parse("01C897F5-67FE-4957-A653-67172C810D46"));
   }

   class EndpointDeclarationWhoseNameStartsWithADigit : ExactlyOnceEndpointDeclaration<EndpointDeclarationWhoseNameStartsWithADigit>, IEndpointIdentity
   {
      public static string Name => "1stBackend";
      public static EndpointId Id { get; } = new(Guid.Parse("2D26F4F6-478C-4CEB-8BA7-C22C28D9BBDA"));
   }

   class EndpointDeclarationWhoseNameExceedsTheCap : ExactlyOnceEndpointDeclaration<EndpointDeclarationWhoseNameExceedsTheCap>, IEndpointIdentity
   {
      public static string Name { get; } = new('A', 29);
      public static EndpointId Id { get; } = new(Guid.Parse("4A142BC8-88BB-476A-9BE3-97C085465F9D"));
   }

   class EndpointDeclarationWhoseNameIsExactlyAtTheCap : ExactlyOnceEndpointDeclaration<EndpointDeclarationWhoseNameIsExactlyAtTheCap>, IEndpointIdentity
   {
      public static string Name { get; } = new('A', 28);
      public static EndpointId Id { get; } = new(Guid.Parse("715B2D00-291A-41B6-8C5F-09E4D3C063A4"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }
}
