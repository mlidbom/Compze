using Compze.Internals.Sql.MySql.Private.DocumentDb;
using Compze.Internals.Sql.MySql.Private.Tessaging;
using Compze.Internals.Sql.MySql.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Internals.Sql.MySql.Private;

class MySqlSqlLayerSchemaManager(IMySqlConnectionPool connectionPool)
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<MySqlSqlLayerSchemaManager>())
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
