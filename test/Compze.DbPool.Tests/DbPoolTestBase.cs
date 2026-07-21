using System.Data.Common;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace Compze.DbPool.Tests;

public abstract class DbPoolTestBase : UniversalTestBase
{
   protected DbPool Pool  { get; }
   protected override void DisposeInternal() => Pool.Dispose();
   readonly IDependencyInjectionContainer _container;

   protected DbPoolTestBase()
   {
      _container = CreateContainer();
      Pool = ResolvePool();
   }

#pragma warning disable CA2000// We are passing this disposable into a constructor of an object we don't own
   protected static IDependencyInjectionContainer CreateContainer() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                                     ._mutate(it => it.Registrar
                                                                                     .CurrentTestsDbPoolIfNotCloneContainer())
                                                                     .Build();
#pragma warning restore CA2000// We are passing this disposable into a constructor of an object we don't own

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   protected DbPool ResolvePool() =>
      _container.Resolve<DbPool>();

   protected static void UseConnection(string reservationName, DbPool pool, Action<DbConnection> func)
   {
      using var connection = CreateConnection(pool.ConnectionStringFor(reservationName));
      connection.Open();
      func(connection);
   }

   ///<summary>Opens the ADO.NET provider's own connection type for the current <see cref="TestEnv.SqlLayer"/> — exactly what a<br/>
   /// <see cref="DbPool"/> consumer does with the connection string the pool hands them.</summary>
   protected static DbConnection CreateConnection(string connectionString) => TestEnv.SqlLayer switch
   {
      SqlLayer.MsSql => new SqlConnection(connectionString),
      SqlLayer.PgSql => new NpgsqlConnection(connectionString),
      SqlLayer.MySql => new MySqlConnection(connectionString),
      SqlLayer.Sqlite or SqlLayer.SqliteMemory => new SqliteConnection(connectionString),
      _ => throw new ArgumentOutOfRangeException()
   };
}
