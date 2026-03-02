using Compze.Sql.Sqlite.Private.DocumentDb;
using Compze.Sql.Sqlite.Private.Tessaging;
using Compze.Sql.Sqlite.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading;
using Compze.Utilities.SystemCE.TransactionsCE;
using System.Threading.Tasks;
using Compze.Threading.TasksCE;

namespace Compze.Sql.Sqlite.Private;

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
