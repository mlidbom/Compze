using Compze.Sql.PostgreSql.Private.DocumentDb;
using Compze.Sql.PostgreSql.Private.Tessaging;
using Compze.Sql.PostgreSql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Sql.PostgreSql.Private;

public class PgSqlSqlLayerSchemaManager(IPgSqlConnectionPool connectionPool)
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<PgSqlSqlLayerSchemaManager>())
         return registrar;
      return registrar.Register(Singleton.For<PgSqlSqlLayerSchemaManager>()
                                         .CreatedBy((IPgSqlConnectionPool connectionPool) => new PgSqlSqlLayerSchemaManager(connectionPool)));
   }

   readonly IPgSqlConnectionPool _connectionPool = connectionPool;

   readonly RunOnceAsync _runOnce = new();

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
   {
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
      {
         await _connectionPool.ExecuteNonQueryAsync($"""

                                                     {PgSqlDocumentDbSqlLayer.SchemaCreationSql}

                                                     {PgSqlInboxSqlLayer.SchemaCreationSql}

                                                     {PgSqlOutboxSqlLayer.SchemaCreationSql}

                                                     {PgSqlTeventStoreSqlLayer.SchemaCreationSql}

                                                     """).caf();
      }).caf();
   }).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
