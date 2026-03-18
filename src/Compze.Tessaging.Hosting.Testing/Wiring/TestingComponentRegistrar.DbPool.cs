using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DbPool.MicrosoftSql;
using Compze.DbPool.MySql;
using Compze.DbPool.PostgreSql;
using Compze.DbPool.Sqlite;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.DbPool;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarDbPool
{
   public static IComponentRegistrar CurrentTestsDbPoolIfNotCloneContainer(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsDbPoolIfNotCloneContainer();

   static IComponentRegistrar CurrentTestsDbPoolIfNotCloneContainer(this TestingComponentRegistrar @this)
   {
      if(@this.IsClone)
      {
         if(!@this.IsRegistered<global::Compze.DbPool.DbPool>())
            throw new Exception("The DbPool must be registered in the root container before any cloning. You cannot register it directly in a cloned container");

         return @this;
      }

      @this.CurrentTestsSerializersIfNotClonedContainer();

      @this.DbPool();
      return TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql        => @this.MsSqlDbPoolSqlLayer(),
         SqlLayer.MySql        => @this.MySqlDbPoolSqlLayer(),
         SqlLayer.PgSql        => @this.PgSqlDbPoolSqlLayer(),
         SqlLayer.Sqlite       => @this.SqliteDbPoolSqlLayer(),
         SqlLayer.SqliteMemory => @this.SqliteMemoryDbPoolSqlLayer(),
         _                     => throw new ArgumentOutOfRangeException()
      };
   }
}
