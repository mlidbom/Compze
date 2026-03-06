using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;

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
      TestEnv.SqlLayer.Must().BeValidEnumValue();
      TestEnv.DIContainer.Must().BeValidEnumValue();

      // Test the ValueForDb functionality (alias for SqlLayer.ValueFor)
      var testValue = TestEnv.SqlLayer.ValueFor(msSql: "SQL Server", mySql: "MySQL", pgSql: "PostgreSQL", sqlite: "SQLite", sqliteMemory: "SQLiteMemory");

      Console.WriteLine($"  ValueForDb result: {testValue}");
      testValue.Must().NotBeNull();

      // Verify the value matches the current sql layer
      var expectedValue = TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql        => "SQL Server",
         SqlLayer.MySql        => "MySQL",
         SqlLayer.PgSql        => "PostgreSQL",
         SqlLayer.Sqlite       => "SQLite",
         SqlLayer.SqliteMemory => "SQLiteMemory",
         _                     => throw new Exception($"Unexpected sql layer: {TestEnv.SqlLayer}")
      };

      testValue.Must().Be(expectedValue);
   }

   [PCT]
   public void ValueFor_on_SqlLayer_returns_the_expected_value()
   {
      // Demonstrate using the extension method directly on the SqlLayer enum
      TestEnv.SqlLayer.ValueFor(
         msSql: 1,
         mySql: 2,
         pgSql: 3,
         sqlite: 4,
         sqliteMemory: 5
      ).Must().Be(TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql        => 1,
         SqlLayer.MySql        => 2,
         SqlLayer.PgSql        => 3,
         SqlLayer.Sqlite       => 4,
         SqlLayer.SqliteMemory => 5,
         _                     => throw new Exception($"Unexpected sql layer: {TestEnv.SqlLayer}")
      });
   }
}
