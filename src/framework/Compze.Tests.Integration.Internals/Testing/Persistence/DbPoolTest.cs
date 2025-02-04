﻿using System;
using Compze.DependencyInjection;
using Compze.Persistence.Common.AdoCE;
using Compze.Persistence.MsSql.SystemExtensions;
using Compze.Persistence.MySql.SystemExtensions;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.Testing;
using Compze.Testing.Persistence;
using Compze.Testing.Persistence.MsSql;
using Compze.Testing.Persistence.MySql;
using Compze.Testing.Persistence.PgSql;

namespace Compze.Tests.Integration.Internals.Testing.Persistence;

public abstract class DbPoolTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   internal static DbPool CreatePool() =>
      TestEnv.PersistenceLayer.Current switch
      {
         PersistenceLayer.MicrosoftSqlServer => new MsSqlDbPool(),
         PersistenceLayer.MySql => new MySqlDbPool(),
         PersistenceLayer.PostgreSql => new PgSqlDbPool(),
         PersistenceLayer.Memory => throw new ArgumentOutOfRangeException(),
         _ => throw new ArgumentOutOfRangeException()
      };

   internal static void UseConnection(string connectionString, DbPool pool, Action<ICompzeDbConnection> func)
   {
      switch(TestEnv.PersistenceLayer.Current)
      {
         case PersistenceLayer.MicrosoftSqlServer:
            UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.PostgreSql:
            UsePgSqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.MySql:
            UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
            break;
         case PersistenceLayer.Memory:
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   static void UseMySqlConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      IMySqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UsePgSqlConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      IPgSqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);

   static void UseMsSqlConnection(string connectionStringFor, Action<ICompzeDbConnection> func) =>
      IMsSqlConnectionPool.CreateInstance(connectionStringFor).UseConnection(func);
}