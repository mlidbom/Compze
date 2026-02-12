using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarDbPool
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotCloneContainer(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsDbPoolIfNotCloneContainer();

   public static IComponentRegistrar CurrentTestsDbPoolIfNotCloneContainer(this TestingComponentRegistrar @this)
   {
      if(@this.Container().IsClone())
      {
         if(!@this.Container().IsRegistered<DbPool>())
            throw new Exception("The DbPool must be registered in the root container before any cloning. You cannot register it directly in a cloned container");

         return @this;
      }

      @this.CurrentTestsSerializersIfNotClonedContainer();

      @this.DbPool();
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MsSql:
            return @this.MsSqlDbPoolSqlLayer();
         case SqlLayer.MySql:
            return @this.MySqlDbPoolSqlLayer();
         case SqlLayer.PgSql:
            return @this.PgSqlDbPoolSqlLayer();
         case SqlLayer.Sqlite:
            return @this.SqliteDbPoolSqlLayer();
         case SqlLayer.SqliteMemory:
            return @this.SqliteMemoryDbPoolSqlLayer();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
