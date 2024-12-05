using System;
using Compze.DependencyInjection;
using Compze.Testing;
using Compze.Testing.Persistence;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Testing.Persistence;

public class After_Creating_Two_Dbs_Named_DB1_And_DB2(string pluggableComponentsCombination) : DbPoolTest(pluggableComponentsCombination)
{
   DbPool _pool;
   const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
   const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";

   [SetUp] public void SetupTask()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
      _pool = CreatePool();
   }

   [Test] public void Connection_to_Db1_can_be_opened_and_used()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      UseConnection(Db1,
                    _pool,
                    connection =>
                    {
                       using var command = connection.CreateCommand();
                       command.CommandText = LayerSpecificCommandText();
                       command.ExecuteScalar().Should().Be(1);
                    });
   }

   [Test] public void Connection_to_Db2_can_be_opened_and_used()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      UseConnection(Db2,
                    _pool,
                    connection =>
                    {
                       using var command = connection.CreateCommand();
                       command.CommandText = LayerSpecificCommandText();
                       command.ExecuteScalar().Should().Be(1);
                    });
   }

   static string LayerSpecificCommandText() => TestEnv.PersistenceLayer.ValueFor(db2: "select 1 from sysibm.sysdummy1", msSql: "select 1", mySql: "select 1", orcl: "select 1 from dual", pgSql: "select 1");

   [Test] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
      _pool.ConnectionStringFor(Db1).Should().Be(_pool.ConnectionStringFor(Db1));
   }

   [Test] public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      _pool.ConnectionStringFor(Db2).Should().Be(_pool.ConnectionStringFor(Db2));
   }

   [Test] public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      _pool.ConnectionStringFor(Db1).Should().NotBe(_pool.ConnectionStringFor(Db2));
   }

   [TearDown] public void TearDownTask()
   {
      if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

      _pool.Dispose();

      // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
      _pool.Invoking(action: _ => _pool.ConnectionStringFor(Db1))
           .Should().Throw<Exception>()
           .Where(exceptionExpression: exception => exception.Message.ToUpperInvariant()
                                                             .Contains("DISPOSED", StringComparison.InvariantCulture));
   }
}
