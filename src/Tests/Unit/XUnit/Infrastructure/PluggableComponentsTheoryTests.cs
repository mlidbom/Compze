using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using FluentAssertions;
using Xunit;

namespace Compze.Tests.Unit.XUnit.Infrastructure;

/// <summary>
/// Test to verify that the PluggableComponentsTheory attribute works correctly.
/// This test should run once for each combination in TestUsingPluggableComponentCombinations.config.
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
      System.Console.WriteLine($"  Sql Layer: {context.SqlLayer}");
      System.Console.WriteLine($"  DI Container: {context.DIContainer}");

      // Verify the values are valid parsed enums (not relying on default check since MicrosoftSqlServer happens to be 0)
      context.SqlLayer.Should().BeOneOf(
         Compze.Wiring.SqlLayer.MicrosoftSqlServer,
         Compze.Wiring.SqlLayer.MySql,
         Compze.Wiring.SqlLayer.PostgreSql,
         Compze.Wiring.SqlLayer.Sqlite,
         Compze.Wiring.SqlLayer.SqliteMemory
      );
      context.DIContainer.Should().BeOneOf(
         Compze.Wiring.DIContainer.Microsoft,
         Compze.Wiring.DIContainer.SimpleInjector
      );

      // Test the ValueForDb functionality (alias for SqlLayer.ValueFor)
      var testValue = context.ValueForDb<string>(
         msSql: "SQL Server",
         mySql: "MySQL",
         pgSql: "PostgreSQL",
         sqlite: "SQLite"
      );

      System.Console.WriteLine($"  ValueForDb result: {testValue}");
      testValue.Should().NotBeNull();

      // Verify the value matches the current sql layer
      var expectedValue = context.SqlLayer switch
      {
         Compze.Wiring.SqlLayer.MicrosoftSqlServer => "SQL Server",
         Compze.Wiring.SqlLayer.MySql => "MySQL",
         Compze.Wiring.SqlLayer.PostgreSql => "PostgreSQL",
         Compze.Wiring.SqlLayer.Sqlite => "SQLite",
         Compze.Wiring.SqlLayer.SqliteMemory => "SQLite",
         _ => throw new System.Exception($"Unexpected sql layer: {context.SqlLayer}")
      };

      testValue.Should().Be(expectedValue);
   }

   [PluggableComponentsTheory]
   public void Can_use_ValueFor_directly_on_SqlLayer(PluggableComponentTestContext context)
   {
      // Demonstrate using the extension method directly on the SqlLayer enum
      var timeout = context.SqlLayer.ValueFor(
         msSql: System.TimeSpan.FromSeconds(5),
         mySql: System.TimeSpan.FromSeconds(10),
         pgSql: System.TimeSpan.FromSeconds(7),
         sqlite: System.TimeSpan.FromSeconds(6)
      );

      System.Console.WriteLine($"✓ Timeout for {context.SqlLayer}: {timeout}");
      
      timeout.Should().BePositive();
      
      // Verify it matches expected value
      var expected = context.SqlLayer switch
      {
         Compze.Wiring.SqlLayer.MicrosoftSqlServer => System.TimeSpan.FromSeconds(5),
         Compze.Wiring.SqlLayer.MySql => System.TimeSpan.FromSeconds(10),
         Compze.Wiring.SqlLayer.PostgreSql => System.TimeSpan.FromSeconds(7),
         Compze.Wiring.SqlLayer.Sqlite => System.TimeSpan.FromSeconds(6),
         Compze.Wiring.SqlLayer.SqliteMemory => System.TimeSpan.FromSeconds(6),
         _ => throw new System.Exception($"Unexpected sql layer: {context.SqlLayer}")
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
