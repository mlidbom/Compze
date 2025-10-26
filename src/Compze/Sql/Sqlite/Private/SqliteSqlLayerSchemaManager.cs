using System.Threading.Tasks;
using Compze.Sql.Sqlite.Private.DocumentDb;
using Compze.Sql.Sqlite.Private.Tessaging;
using Compze.Sql.Sqlite.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Sql.Sqlite.Private;

class SqliteSqlLayerSchemaManager(ISqliteConnectionPool connectionPool)
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<SqliteSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<SqliteSqlLayerSchemaManager>()
                                         .CreatedBy((ISqliteConnectionPool connectionPool) => new SqliteSqlLayerSchemaManager(connectionPool)));
   }

   bool _initialized = false;
   readonly ISqliteConnectionPool _connectionPool = connectionPool;

   public async Task EnsureSchemaInitializedAsync()
   {
      if(!_initialized)
      {
         await TransactionScopeCe.SuppressAmbientAsync(async () =>
         {
            //todo: test if running them in parallel is faster
            await _connectionPool.ExecuteNonQueryAsync($"""

                                                        {SqliteDocumentDbSqlLayer.SchemaCreationSql}

                                                        {SqliteInboxSqlLayer.SchemaCreationSql}

                                                        {SqliteOutboxSqlLayer.SchemaCreationSql}

                                                        {SqliteTeventStoreSqlLayer.SchemaCreationSql}

                                                        """).caf();
         }).caf();
         _initialized = true;
      }
   }

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
