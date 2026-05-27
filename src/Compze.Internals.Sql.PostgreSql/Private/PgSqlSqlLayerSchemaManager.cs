using Compze.Internals.Sql.PostgreSql.Private.DocumentDb;
using Compze.Internals.Sql.PostgreSql.Private.Tessaging;
using Compze.Internals.Sql.PostgreSql.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Internals.Sql.PostgreSql.Private;

class PgSqlSqlLayerSchemaManager(IPgSqlConnectionPool connectionPool)
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<PgSqlSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<PgSqlSqlLayerSchemaManager>()
                                         .CreatedBy((IPgSqlConnectionPool connectionPool) => new PgSqlSqlLayerSchemaManager(connectionPool))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly IPgSqlConnectionPool _connectionPool = connectionPool;

   readonly RunOnceAsync _runOnce = new();

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
   {
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
      {
         await _connectionPool.ExecuteNonQueryAsync($"""

                                                     {TypeIdInterning.PgSqlTypeIdInternerPersistence.SchemaCreationSql}

                                                     {PgSqlDocumentDbSqlLayer.SchemaCreationSql}

                                                     {PgSqlInboxSqlLayer.SchemaCreationSql}

                                                     {PgSqlOutboxSqlLayer.SchemaCreationSql}

                                                     {PgSqlTeventStoreSqlLayer.SchemaCreationSql}

                                                     """).caf();
      }).caf();
   }).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
