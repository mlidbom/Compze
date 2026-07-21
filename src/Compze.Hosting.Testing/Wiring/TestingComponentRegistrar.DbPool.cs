using Compze.DbPool.MicrosoftSql;
using Compze.DbPool.MySql;
using Compze.DbPool.PostgreSql;
using Compze.DbPool.Sqlite;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.DbPool;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarDbPool
{
   ///<summary>Registers the test database pool and the current test's serializers, unless this is a cloned container (clones inherit them from the root container).</summary>
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

      // Idempotent: a container can register more than one pooled database (e.g. business data plus the type-id
      // interner's own database), and each pool registrar calls this to ensure the shared DbPool exists. Only the
      // first call sets up the serializers, the DbPool, and its SQL layer; later calls find it already registered.
      if(@this.IsRegistered<global::Compze.DbPool.DbPool>())
         return @this;

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
