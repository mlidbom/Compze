using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Sql.MicrosoftSql.Private.DbPool;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite.DbPool;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarDbPool
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered(this IComponentRegistrar register) => 
      register.CastTo<TestingComponentRegistrar>().CurrentTestsDbPoolIfNotAlreadyRegistered();

   public static IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered(this TestingComponentRegistrar @this)
   {
      @this.DbPoolIfNotAlreadyRegistered();
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            return @this.MsSqlDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.MySql:
            return @this.MySqlDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.PostgreSql:
            return @this.PgSqlDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.Sqlite:
            return @this.SqliteDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.SqliteMemory:
            return  @this.SqliteMemoryDbPoolSqlLayerIfNotAlreadyRegistered();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
