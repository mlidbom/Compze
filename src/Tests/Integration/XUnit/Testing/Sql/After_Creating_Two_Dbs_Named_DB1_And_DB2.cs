using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Common.Testing.Sql;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Testing.DbPool;
using Compze.Wiring;
using FluentAssertions;

namespace Compze.Tests.Integration.XUnit.Testing.Sql;

public class After_Creating_Two_Dbs_Named_DB1_And_DB2 : DbPoolTestBase
{
   readonly DbPool _pool = CreatePool();
   const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
   const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";

   [PCT] public void Connection_to_Db1_can_be_opened_and_used()
   {
      UseConnection(Db1,
                    _pool,
                    connection =>
                    {
                       using var command = connection.CreateCommand();
                       command.CommandText = LayerSpecificCommandText();
                       command.ExecuteScalar().Should().Be(1);
                    });
   }

   [PCT] public void Connection_to_Db2_can_be_opened_and_used()
   {
      UseConnection(Db2,
                    _pool,
                    connection =>
                    {
                       using var command = connection.CreateCommand();
                       command.CommandText = LayerSpecificCommandText();
                       command.ExecuteScalar().Should().Be(1);
                    });
   }

   static string LayerSpecificCommandText() => TestEnv.SqlLayer.ValueFor(msSql: "select 1", mySql: "select 1", pgSql: "select 1", sqlite: "select 1");

   [PCT] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
   {
      _pool.ConnectionStringFor(Db1).Should().Be(_pool.ConnectionStringFor(Db1));
   }

   [PCT] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
   {
      _pool.ConnectionStringFor(Db2).Should().Be(_pool.ConnectionStringFor(Db2));
   }

   [PCT] public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
   {
      _pool.ConnectionStringFor(Db1).Should().NotBe(_pool.ConnectionStringFor(Db2));
   }

   public override void Dispose()
   {
      _pool.Dispose();
      base.Dispose();
      // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
      _pool.Invoking(action: _ => _pool.ConnectionStringFor(Db1))
           .Should().Throw<Exception>()
           .Where(exceptionExpression: exception => exception.Message.ToUpperInvariant()
                                                             .Contains("DISPOSED", StringComparison.InvariantCulture));
   }
}
