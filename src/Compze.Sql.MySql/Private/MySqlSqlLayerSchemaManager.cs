using System.Threading.Tasks;
using Compze.Sql.MySql.Private.DocumentDb;
using Compze.Sql.MySql.Private.Tessaging;
using Compze.Sql.MySql.Private.TEventStore;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Sql.MySql.Private;

class MySqlSqlLayerSchemaManager(IMySqlConnectionPool connectionPool)
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<MySqlSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<MySqlSqlLayerSchemaManager>()
                                         .CreatedBy((IMySqlConnectionPool connectionPool) => new MySqlSqlLayerSchemaManager(connectionPool))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly IMySqlConnectionPool _connectionPool = connectionPool;

   readonly RunOnceAsync _runOnce = new();

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
   {
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
      {
         await _connectionPool.ExecuteNonQueryAsync($"""

                                                     {MySqlDocumentDbSqlLayer.SchemaCreationSql}

                                                     {MySqlInboxSqlLayer.SchemaCreationSql}

                                                     {MySqlOutboxSqlLayer.SchemaCreationSql}

                                                     {MySqlTeventStoreSqlLayer.SchemaCreationSql}

                                                     """).caf();
      }).caf();
   }).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
