using Compze.Internals.Sql.Sqlite.Private.DocumentDb;
using Compze.Internals.Sql.Sqlite.Private.Tessaging;
using Compze.Internals.Sql.Sqlite.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Internals.Sql.Sqlite.Private;

class SqliteSqlLayerSchemaManager(ISqliteConnectionPool connectionPool)
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<SqliteSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<SqliteSqlLayerSchemaManager>()
                                         .CreatedBy((ISqliteConnectionPool connectionPool) => new SqliteSqlLayerSchemaManager(connectionPool))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly ISqliteConnectionPool _connectionPool = connectionPool;

   readonly RunOnceAsync _runOnce = new();

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
   {
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
      {
         await _connectionPool.ExecuteNonQueryAsync($"""

                                                     {SqliteDocumentDbSqlLayer.SchemaCreationSql}

                                                     {SqliteInboxSqlLayer.SchemaCreationSql}

                                                     {SqliteOutboxSqlLayer.SchemaCreationSql}

                                                     {SqliteTeventStoreSqlLayer.SchemaCreationSql}

                                                     """).caf();
      }).caf();
   }).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
