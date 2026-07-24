using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.Sqlite._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Microsoft.Data.Sqlite;
using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite._private;

partial class SqliteEndpointCatalogSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

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

   //Sqlite has no server sessions to scope a lock to, so the lock is an OS mutex keyed on the database's identity instead
   //(see SqliteEndpointProcessLockHold). A live holder cannot lose an OS mutex, so onLockLostWhileHeld can never fire.
   public async Task<ITessagingSqlLayer.IEndpointProcessLockHold?> TryTakeProcessLockAsync(string endpointName, string lockHolderDescription, Action<Exception> onLockLostWhileHeld)
   {
      var hold = await SqliteEndpointProcessLockHold.TryTakeAsync(_connectionFactory.ConnectionString, endpointName).caf();
      if(hold == null) return null;

      try
      {
         //Stamped once the mutex is held, before the hold is handed back: the live lock and its recorded holder become one
         //fact for any process that later reads the row after being refused this lock. Unlike the server engines, the mutex
         //is not a database session, so the stamp cannot ride the lock's own connection - it is the next thing done while
         //holding it.
         await RecordLockHolderAsync(endpointName, lockHolderDescription).caf();
         return hold;
      }
      catch //Resource cleanup, not handling: a stamp that fails must not leave the mutex held with no hold to release it.
      {
         await hold.DisposeAsync().caf();
         throw;
      }
   }

   async Task RecordLockHolderAsync(string endpointName, string lockHolderDescription) =>
      await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LockHolderDescription} = @{Catalog.LockHolderDescription}
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddMediumTextParameter(Catalog.LockHolderDescription, lockHolderDescription)
                   .ExecuteNonQueryAsync().caf()).caf();

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<SqliteCommand> addFilterParameter) =>
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var entries = new List<ITessagingSqlLayer.EndpointCatalogEntry>();

            command.SetCommandText(
               $"""

                SELECT {Catalog.EndpointName}, {Catalog.EndpointId}, {Catalog.LockHolderDescription}
                FROM {Catalog.TableName} {filterClause}

                """);
            addFilterParameter(command);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               entries.Add(new ITessagingSqlLayer.EndpointCatalogEntry(
                              endpointName: reader.GetString(0),
                              endpointId: new EndpointId(reader.GetGuidFromString(1)),
                              lockHolderDescription: await reader.IsDBNullAsync(2).caf() ? null : reader.GetString(2)));
            }

            return entries;
         }).caf();

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
