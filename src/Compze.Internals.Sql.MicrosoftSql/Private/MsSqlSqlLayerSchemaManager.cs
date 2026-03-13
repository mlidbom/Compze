using Compze.Internals.Sql.MicrosoftSql.Private.DocumentDb;
using Compze.Internals.Sql.MicrosoftSql.Private.Tessaging;
using Compze.Internals.Sql.MicrosoftSql.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Internals.Sql.MicrosoftSql.Private;

class MsSqlSqlLayerSchemaManager(IMsSqlConnectionPool connectionPool)
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<MsSqlSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<MsSqlSqlLayerSchemaManager>()
                                         .CreatedBy((IMsSqlConnectionPool connectionPool) => new MsSqlSqlLayerSchemaManager(connectionPool))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly IMsSqlConnectionPool _connectionPool = connectionPool;

   readonly RunOnceAsync _runOnce = new();

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
   {
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
      {
         await _connectionPool.ExecuteNonQueryAsync(
            $"""

             {MsSqlDocumentDbSqlLayer.SchemaCreationSql}

             {MsSqlInboxSqlLayer.SchemaCreationSql}

             {MsSqlOutboxSqlLayer.SchemaCreationSql}

             {MsSqlTeventStoreSqlLayer.SchemaCreationSql}

             """).caf();
      }).caf();
   }).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
