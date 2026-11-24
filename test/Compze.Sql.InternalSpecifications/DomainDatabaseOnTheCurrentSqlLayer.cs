using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql._internal;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql._internal;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite._internal;
using Compze.Sql.Sqlite.Wiring;
using Compze.Tests.Infrastructure;

namespace Compze.Sql.InternalSpecifications;

///<summary>A domain database on the sql layer the current test runs on, in a container of its own: what a specification needs to run<br/>
/// the same SQL on every sql layer, through the layer's connection pool, or to resolve the pool and ask in the layer's own words.</summary>
sealed class DomainDatabaseOnTheCurrentSqlLayer : IAsyncDisposable
{
   DomainDatabaseOnTheCurrentSqlLayer(IDependencyInjectionContainer container) => Container = container;

   ///<summary>The domain database <paramref name="databaseName"/> on the sql layer the current test runs on.</summary>
   internal static DomainDatabaseOnTheCurrentSqlLayer Named(string databaseName)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      _ = TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql        => builder.Registrar.MsSqlDomainDatabase(databaseName),
         SqlLayer.MySql        => builder.Registrar.MySqlDomainDatabase(databaseName),
         SqlLayer.PgSql        => builder.Registrar.PgSqlDomainDatabase(databaseName),
         SqlLayer.Sqlite       => builder.Registrar.SqliteDomainDatabase(databaseName),
         SqlLayer.SqliteMemory => builder.Registrar.SqliteMemoryDomainDatabase(databaseName),
         _                     => throw new ArgumentOutOfRangeException(nameof(SqlLayer), TestEnv.SqlLayer, null)
      };
      return new DomainDatabaseOnTheCurrentSqlLayer(builder.Build());
   }

   ///<summary>The container holding the database, which resolves the sql layer's connection pool, such as <see cref="ISqliteConnectionPool"/>.</summary>
   internal IDependencyInjectionContainer Container { get; }

   ///<summary>Runs <paramref name="commandText"/> on a connection from the sql layer's connection pool: the ambient transaction's, if there is one.</summary>
   internal int ExecuteNonQuery(string commandText) =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql                             => Container.Resolve<IMsSqlConnectionPool>().ExecuteNonQuery(commandText),
         SqlLayer.MySql                             => Container.Resolve<IMySqlConnectionPool>().ExecuteNonQuery(commandText),
         SqlLayer.PgSql                             => Container.Resolve<IPgSqlConnectionPool>().ExecuteNonQuery(commandText),
         SqlLayer.Sqlite or SqlLayer.SqliteMemory => Container.Resolve<ISqliteConnectionPool>().ExecuteNonQuery(commandText),
         _                                          => throw new ArgumentOutOfRangeException(nameof(SqlLayer), TestEnv.SqlLayer, null)
      };

   ///<summary>As <see cref="ExecuteNonQuery"/>, returning the first column of the first row.</summary>
   internal object? ExecuteScalar(string commandText) =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql                             => Container.Resolve<IMsSqlConnectionPool>().ExecuteScalar(commandText),
         SqlLayer.MySql                             => Container.Resolve<IMySqlConnectionPool>().ExecuteScalar(commandText),
         SqlLayer.PgSql                             => Container.Resolve<IPgSqlConnectionPool>().ExecuteScalar(commandText),
         SqlLayer.Sqlite or SqlLayer.SqliteMemory => Container.Resolve<ISqliteConnectionPool>().ExecuteScalar(commandText),
         _                                          => throw new ArgumentOutOfRangeException(nameof(SqlLayer), TestEnv.SqlLayer, null)
      };

   public ValueTask DisposeAsync() => Container.DisposeAsync();
}
