using Compze.Sql.MicrosoftSql.Private.DocumentDb;
using Compze.Sql.MicrosoftSql.Private.Tessaging;
using Compze.Sql.MicrosoftSql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading;
using Compze.Utilities.SystemCE.TransactionsCE;
using System.Threading.Tasks;
using Compze.Threading.TasksCE;

namespace Compze.Sql.MicrosoftSql.Private;

public class MsSqlSqlLayerSchemaManager(IMsSqlConnectionPool connectionPool)
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
