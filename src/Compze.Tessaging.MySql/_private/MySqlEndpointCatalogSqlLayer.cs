using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.MySql._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Threading;
using MySqlConnector;
using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlEndpointCatalogSqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager, IDatabaseProcessLocks processLocks) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly IDatabaseProcessLocks _processLocks = processLocks;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddMediumTextParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddParameter("filter", endpointId.Value)).caf()).SingleOrDefault();

   public async Task<bool> TryInsertEntryAsync(string endpointName, EndpointId endpointId, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //INSERT IGNORE is MySQL's race-safe insert-if-absent: a racing registration's row makes this one
                       //affect zero rows instead of erroring. The caveat - it would also swallow an EndpointId-uniqueness
                       //violation - is covered by the id-consistency read the process lock performs before inserting.
                       $"""

                        INSERT IGNORE INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc})
                            VALUES (@{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc})

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.EndpointId, endpointId.Value)
                   .AddDateTime2Parameter(Catalog.CreatedUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task<IAsyncDisposable?> TryTakeProcessLockAsync(string lockName, string purpose, Action<Exception> onLockLostWhileHeld) =>
      await _processLocks.TryTakeAsync(lockName, purpose, WaitTimeout.Zero, onLockLostWhileHeld).caf();

   public async Task<string?> ProcessLockHolderAsync(string lockName) => await _processLocks.HolderOfAsync(lockName).caf();

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<MySqlCommand> addFilterParameter) =>
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var entries = new List<ITessagingSqlLayer.EndpointCatalogEntry>();

            command.SetCommandText(
               $"""

                SELECT {Catalog.EndpointName}, {Catalog.EndpointId}
                FROM {Catalog.TableName} {filterClause}

                """);
            addFilterParameter(command);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               entries.Add(new ITessagingSqlLayer.EndpointCatalogEntry(
                              endpointName: reader.GetString(0),
                              endpointId: new EndpointId(reader.GetGuid(1))));
            }

            return entries;
         }).caf();

   public Task InitAsync()
   {
      _schemaManager.EnsureSchemaInitialized();
      return Task.CompletedTask;
   }
}
