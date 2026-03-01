using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

internal static class TestingComponentRegistrarTestingSqlLayerRegistrar
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
