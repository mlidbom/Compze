using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.Must;

namespace Compze.DbPool.Tests;

public class After_Creating_Two_Dbs_Named_DB1_And_DB2 : DbPoolTestBase
{
   const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
   const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";

   [PCT] public void Connection_to_Db1_can_be_opened_and_used()
   {
      UseConnection(Db1,
                    Pool,
                    connection =>
                    {
                       using var command = connection.CreateCommand();
                       command.CommandText = LayerSpecificCommandText();
                       command.ExecuteScalar().Must().Be("1");
                    });
   }

   [PCT] public void Connection_to_Db2_can_be_opened_and_used()
   {
      UseConnection(Db2,
                    Pool,
                    connection =>
                    {
                       using var command = connection.CreateCommand();
                       command.CommandText = LayerSpecificCommandText();
                       command.ExecuteScalar().Must().Be("1");
                    });
   }

   static string LayerSpecificCommandText() => TestEnv.SqlLayer.ValueFor(msSql: "select '1'", mySql: "select '1'", pgSql: "select '1'", sqlite: "select '1'", sqliteMemory: "select '1'");

   [PCT] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1() =>
      Pool.ConnectionStringFor(Db1).Must().Be(Pool.ConnectionStringFor(Db1));

   [PCT] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2() =>
      Pool.ConnectionStringFor(Db2).Must().Be(Pool.ConnectionStringFor(Db2));

   [PCT] public void The_Db1_connection_string_is_different_from_the_Db2_connection_string() =>
      Pool.ConnectionStringFor(Db1).Must().NotBe(Pool.ConnectionStringFor(Db2));

   [PCT] public void Using_disposed_pool_throws_Exception()
   {
      var disposedPool = ResolvePool();
      disposedPool.Dispose();
      var exception = disposedPool.Invoking(action: _ => disposedPool.ConnectionStringFor(Db1))
                                  .Must().Throw<Exception>();
      exception.Which.Message.ToUpperInvariant().Must().Contain("DISPOSED");
   }
}
