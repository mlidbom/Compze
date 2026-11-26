using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.Sqlite._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Threading;
using Microsoft.Data.Sqlite;
using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite._private;

partial class SqliteEndpointCatalogSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager, IDatabaseProcessLocks processLocks) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly IDatabaseProcessLocks _processLocks = processLocks;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddMediumTextParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddMediumTextParameter("filter", endpointId.ToString())).caf()).SingleOrDefault();

   public async Task<bool> TryInsertEntryAsync(string endpointName, EndpointId endpointId, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //Race-safe as it stands: sqlite serializes writers, so a racing registration's insert and this one's
                       //NOT EXISTS check cannot interleave.
                       $"""

                        INSERT INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc})
                             SELECT @{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc}
                        WHERE NOT EXISTS (SELECT 1 FROM {Catalog.TableName} WHERE {Catalog.EndpointName} = @{Catalog.EndpointName})

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddMediumTextParameter(Catalog.EndpointId, endpointId.ToString())
                   .AddDateTime2Parameter(Catalog.CreatedUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task<IAsyncDisposable?> TryTakeProcessLockAsync(string lockName, string purpose, Action<Exception> onLockLostWhileHeld) =>
      await _processLocks.TryTakeAsync(lockName, purpose, WaitTimeout.Zero, onLockLostWhileHeld).caf();

   public async Task<string?> ProcessLockHolderAsync(string lockName) => await _processLocks.HolderOfAsync(lockName).caf();

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<SqliteCommand> addFilterParameter) =>
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
                              endpointId: new EndpointId(reader.GetGuidFromString(1))));
            }

            return entries;
         }).caf();

   public Task InitAsync()
   {
      _schemaManager.EnsureSchemaInitialized();
      return Task.CompletedTask;
   }
}
