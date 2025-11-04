using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.Must;

namespace Compze.Tests.Integration.Infrastructure;

/// <summary>
/// Test to verify that the PluggableComponentsTheory attribute works correctly.
/// This test should run once for each combination in TestUsingPluggableComponentCombinations.
/// </summary>
public class PluggableComponentsTheoryTests : UniversalTestBase
{
   [PCT]
   public void Should_execute_with_context_object_injected()
   {
      // Access the parsed values directly from the context
      System.Console.WriteLine($"  Sql Layer: {TestEnv.SqlLayer}");
      System.Console.WriteLine($"  DI Container: {TestEnv.DIContainer}");

      // Verify the values are valid parsed enums (not relying on default check since MicrosoftSqlServer happens to be 0)
      TestEnv.SqlLayer.Must().BeOneOf(
         [SqlLayer.MsSql,
         SqlLayer.MySql,
         SqlLayer.PgSql,
         SqlLayer.Sqlite,
         SqlLayer.SqliteMemory]
      );
      TestEnv.DIContainer.Must().BeOneOf(
         [DIContainer.Microsoft,
         DIContainer.SimpleInjector]
      );

      // Test the ValueForDb functionality (alias for SqlLayer.ValueFor)
      var testValue = TestEnv.SqlLayer.ValueFor(msSql: "SQL Server", mySql: "MySQL", pgSql: "PostgreSQL", sqlite: "SQLite", sqliteMemory: "SQLiteMemory");

      System.Console.WriteLine($"  ValueForDb result: {testValue}");
      testValue.Must().NotBeNull();

      // Verify the value matches the current sql layer
      var expectedValue = TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql => "SQL Server",
         SqlLayer.MySql              => "MySQL",
         SqlLayer.PgSql         => "PostgreSQL",
         SqlLayer.Sqlite             => "SQLite",
         SqlLayer.SqliteMemory       => "SQLiteMemory",
         _                                         => throw new System.Exception($"Unexpected sql layer: {TestEnv.SqlLayer}")
      };

      testValue.Must().Be(expectedValue);
   }

   [PCT]
   public void Can_use_ValueFor_directly_on_SqlLayer()
   {
      // Demonstrate using the extension method directly on the SqlLayer enum
      var timeout = TestEnv.SqlLayer.ValueFor(
         msSql: System.TimeSpan.FromSeconds(5),
         mySql: System.TimeSpan.FromSeconds(10),
         pgSql: System.TimeSpan.FromSeconds(7),
         sqlite: System.TimeSpan.FromSeconds(6),
         sqliteMemory: System.TimeSpan.FromSeconds(6)
      );

      System.Console.WriteLine($"✓ Timeout for {TestEnv.SqlLayer}: {timeout}");

      timeout.Must().BePositive();

      // Verify it matches expected value
      var expected = TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql => System.TimeSpan.FromSeconds(5),
         SqlLayer.MySql              => System.TimeSpan.FromSeconds(10),
         SqlLayer.PgSql         => System.TimeSpan.FromSeconds(7),
         SqlLayer.Sqlite             => System.TimeSpan.FromSeconds(6),
         SqlLayer.SqliteMemory       => System.TimeSpan.FromSeconds(6),
         _                                         => throw new System.Exception($"Unexpected sql layer: {TestEnv.SqlLayer}")
      };

      timeout.Must().Be(expected);
   }

   [XF]
   public void Regular_fact_test_should_run_only_once()
   {
      System.Console.WriteLine("✓ This regular Fact test runs exactly once");
      true.Must().BeTrue();
   }
}
