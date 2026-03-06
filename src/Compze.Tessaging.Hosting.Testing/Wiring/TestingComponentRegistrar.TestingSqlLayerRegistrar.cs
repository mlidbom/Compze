using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarTestingSqlLayerRegistrar
{
   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register, string connectionStringName) =>
      register.CastTo<TestingComponentRegistrar>()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);

   static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this TestingComponentRegistrar @this, string connectionStringName)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MsSql:
            return @this.MsSqlConnectionPool(connectionStringName)
                        .MsSqlSqlLayers();
         case SqlLayer.MySql:
            return @this.MySqlConnectionPool(connectionStringName)
                        .MySqlSqlLayers();
         case SqlLayer.PgSql:
            return @this.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
                        .PgSqlSqlLayers();
         case SqlLayer.Sqlite:
            return @this.SqliteConnectionPool(connectionStringName)
                        .SqliteDSqliteSqlLayers();
         case SqlLayer.SqliteMemory:
            return @this.SqliteMemoryConnectionPool(connectionStringName)
                        .SqliteDSqliteSqlLayers();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
