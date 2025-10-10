using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using FluentAssertions;
using Xunit;

namespace Compze.Tests.Unit.XUnit.Infrastructure;

/// <summary>
/// Test to verify that the PluggableComponentsTheory attribute works correctly.
/// This test should run once for each combination in TestUsingPluggableComponentCombinations.
/// </summary>
public class PluggableComponentsTheoryTests : DuplicateByPluggableComponentTest
{
   [PluggableComponentsTheory]
   public void Should_execute_with_context_object_injected(PluggableComponentTestContext context)
   {
      // Output the combination so we can see it ran
      System.Console.WriteLine($"✓ Test executed with context: {context.Combination}");

      // Verify the context is not null
      context.Should().NotBeNull();

      // Access the parsed values directly from the context
      System.Console.WriteLine($"  Persistence Layer: {context.PersistenceLayer}");
      System.Console.WriteLine($"  DI Container: {context.DIContainer}");

      // Verify the values are valid parsed enums (not relying on default check since MicrosoftSqlServer happens to be 0)
      context.PersistenceLayer.Should().BeOneOf(
         Compze.Wiring.PersistenceLayer.MicrosoftSqlServer,
         Compze.Wiring.PersistenceLayer.Memory,
         Compze.Wiring.PersistenceLayer.MySql,
         Compze.Wiring.PersistenceLayer.PostgreSql
      );
      context.DIContainer.Should().BeOneOf(
         Compze.Wiring.DIContainer.Microsoft,
         Compze.Wiring.DIContainer.SimpleInjector
      );

      // Test the ValueForDb functionality (alias for PersistenceLayer.ValueFor)
      var testValue = context.ValueForDb<string>(
         msSql: "SQL Server",
         memory: "In-Memory",
         mySql: "MySQL",
         pgSql: "PostgreSQL"
      );

      System.Console.WriteLine($"  ValueForDb result: {testValue}");
      testValue.Should().NotBeNull();

      // Verify the value matches the current persistence layer
      var expectedValue = context.PersistenceLayer switch
      {
         Compze.Wiring.PersistenceLayer.MicrosoftSqlServer => "SQL Server",
         Compze.Wiring.PersistenceLayer.Memory => "In-Memory",
         Compze.Wiring.PersistenceLayer.MySql => "MySQL",
         Compze.Wiring.PersistenceLayer.PostgreSql => "PostgreSQL",
         _ => throw new System.Exception($"Unexpected persistence layer: {context.PersistenceLayer}")
      };

      testValue.Should().Be(expectedValue);
   }

   [PluggableComponentsTheory]
   public void Can_use_ValueFor_directly_on_PersistenceLayer(PluggableComponentTestContext context)
   {
      // Demonstrate using the extension method directly on the PersistenceLayer enum
      var timeout = context.PersistenceLayer.ValueFor(
         msSql: System.TimeSpan.FromSeconds(5),
         memory: System.TimeSpan.FromSeconds(1),
         mySql: System.TimeSpan.FromSeconds(10),
         pgSql: System.TimeSpan.FromSeconds(7)
      );

      System.Console.WriteLine($"✓ Timeout for {context.PersistenceLayer}: {timeout}");
      
      timeout.Should().BePositive();
      
      // Verify it matches expected value
      var expected = context.PersistenceLayer switch
      {
         Compze.Wiring.PersistenceLayer.MicrosoftSqlServer => System.TimeSpan.FromSeconds(5),
         Compze.Wiring.PersistenceLayer.Memory => System.TimeSpan.FromSeconds(1),
         Compze.Wiring.PersistenceLayer.MySql => System.TimeSpan.FromSeconds(10),
         Compze.Wiring.PersistenceLayer.PostgreSql => System.TimeSpan.FromSeconds(7),
         _ => throw new System.Exception($"Unexpected persistence layer: {context.PersistenceLayer}")
      };
      
      timeout.Should().Be(expected);
   }

   [Fact]
   public void Regular_fact_test_should_run_only_once()
   {
      System.Console.WriteLine("✓ This regular Fact test runs exactly once");
      true.Should().BeTrue();
   }
}
