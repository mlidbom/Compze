using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.DocumentDb.MicrosoftSql.Wiring;
using Compze.DocumentDb.MySql.Wiring;
using Compze.DocumentDb.PostgreSql.Wiring;
using Compze.DocumentDb.Sqlite.Wiring;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.MicrosoftSql.Wiring;
using Compze.Tessaging.MySql.Wiring;
using Compze.Tessaging.PostgreSql.Wiring;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.Teventive.TeventStore.MicrosoftSql.Wiring;
using Compze.Tessaging.Teventive.TeventStore.MySql.Wiring;
using Compze.Tessaging.Teventive.TeventStore.PostgreSql.Wiring;
using Compze.Tessaging.Teventive.TeventStore.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarSqlLayer
{
   ///<summary>
   /// Registers, for the SQL backend the current test runs against, the persistence the Tessaging vertical's
   /// storage stack uses: the type-id interner, the document db, the tessaging inbox/outbox, and the tevent store —
   /// including one schema manager that creates all of their schemas.
   ///</summary>
   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register, string connectionStringName) =>
      register.CastTo<TestingComponentRegistrar>()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);

   static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this TestingComponentRegistrar @this, string connectionStringName) =>
      TestEnv.SqlLayer switch
      {
         SqlLayer.MsSql => @this.MsSqlEndpointDatabase(connectionStringName)
                                .MsSqlDocumentDbSqlLayer()
                                .MsSqlTessagingSqlLayer()
                                .MsSqlTeventStoreSqlLayer(),
         SqlLayer.MySql => @this.MySqlEndpointDatabase(connectionStringName)
                                .MySqlDocumentDbSqlLayer()
                                .MySqlTessagingSqlLayer()
                                .MySqlTeventStoreSqlLayer(),
         SqlLayer.PgSql => @this.PgSqlEndpointDatabase(connectionStringName)
                                .PgSqlDocumentDbSqlLayer()
                                .PgSqlTessagingSqlLayer()
                                .PgSqlTeventStoreSqlLayer(),
         SqlLayer.Sqlite => @this.SqliteEndpointDatabase(connectionStringName)
                                 .SqliteTypeIdInterner($"{connectionStringName}.TypeIdInterner")
                                 .SqliteDocumentDbSqlLayer()
                                 .SqliteTessagingSqlLayer()
                                 .SqliteTeventStoreSqlLayer(),
         SqlLayer.SqliteMemory => @this.SqliteMemoryConnectionPool(connectionStringName)
                                       .SqliteTypeIdInterner($"{connectionStringName}.TypeIdInterner")
                                       .SqliteDocumentDbSqlLayer()
                                       .SqliteTessagingSqlLayer()
                                       .SqliteTeventStoreSqlLayer(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
