﻿using System;
using System.Data;
using System.Linq;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.Persistence.PgSql.Testing.Databases;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies
{
    //Urgent: Remove this once we have all the persistence layers working.
    [TestFixture] public class ExplorePersistenceLayerAdoImplementations
    {
        MsSqlDatabasePool _msSqlPool;
        MySqlDatabasePool _mySqlPool;
        MsSqlConnectionProvider _msSqlConnection;
        MySqlConnectionProvider _mySqlConnection;
        PgSqlDatabasePool _pgSqlPool;
        PgSqlConnectionProvider _pgSqlConnection;

        [Test] public void MsSqlRoundtrip()
        {
            var result = _msSqlConnection.UseCommand(
                command => command.SetCommandText("select @parm")
                                  .AddNullableParameter("parm", SqlDbType.Decimal, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToSqlDecimal())
                                  .ExecuteReaderAndSelect(@this => @this.GetSqlDecimal(0))
                                  .Single());

            Console.WriteLine(result.ToString());
        }

        [Test] public void MySqlRoundtrip()
        {
            var result = _mySqlConnection.UseCommand(
                command => command.SetCommandText($"select cast(cast(@parm as {EventTable.ReadOrderType}) as char(39))")
                                  .AddNullableParameter("parm", MySqlDbType.VarChar, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToString())
                                  .ExecuteReaderAndSelect(@this => @this.GetString(0))
                                  .Single());

            Console.WriteLine(result);
        }

        [Test] public void PgSqlRoundtrip()
        {
            var result = _pgSqlConnection.UseCommand(
                command => command.SetCommandText($"select cast(cast(@parm as {EventTable.ReadOrderType}) as char(39))")
                                  .AddNullableParameter("parm", NpgsqlDbType.Varchar, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToString())
                                  .ExecuteReaderAndSelect(@this => @this.GetString(0))
                                  .Single());

            Console.WriteLine(result);
        }

        [SetUp] public void SetupTask()
        {
            _msSqlPool = new MsSqlDatabasePool();
            _msSqlConnection = new MsSqlConnectionProvider(_msSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            _mySqlPool = new MySqlDatabasePool();
            _mySqlConnection = new MySqlConnectionProvider(_mySqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            _pgSqlPool = new PgSqlDatabasePool();
            _pgSqlConnection = new PgSqlConnectionProvider(_pgSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));
        }

        [TearDown] public void TearDownTask()
        {
            _msSqlPool.Dispose();
            _mySqlPool.Dispose();
            _pgSqlPool.Dispose();
        }
    }
}
