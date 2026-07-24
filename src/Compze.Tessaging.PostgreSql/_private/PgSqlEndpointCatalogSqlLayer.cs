using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.PostgreSql._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Npgsql;
using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql._private;

partial class PgSqlEndpointCatalogSqlLayer(IPgSqlConnectionPool connectionFactory, PgSqlSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddMediumTextParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddParameter("filter", endpointId.Value)).caf()).SingleOrDefault();

   public async Task<bool> TryInsertEntryAsync(string endpointName, EndpointId endpointId, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //ON CONFLICT DO NOTHING targets the name key precisely: a racing registration's row makes this one
                       //affect zero rows, while an EndpointId-uniqueness violation still fails loud.
                       $"""

                        INSERT INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc})
                            VALUES (@{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc})
                        ON CONFLICT ({Catalog.EndpointName}) DO NOTHING

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.EndpointId, endpointId.Value)
                   .AddTimestampWithTimeZone(Catalog.CreatedUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   //The lock is a session-scoped advisory lock on a dedicated connection: it lives exactly as long as the session, so a
   //crashed process's lock is released when the server notices its connection die, and no pause can lose a live holder's
   //lock. Advisory lock keys are 64-bit integers; the database name joins the hash input so identical endpoint names in
   //different domain databases can never collide, whatever the server's lock-key scoping.
   public async Task<ITessagingSqlLayer.IEndpointProcessLockHold?> TryTakeProcessLockAsync(string endpointName, string lockHolderDescription, Action<Exception> onLockLostWhileHeld)
   {
      //Pooling would break the lock's session-lifetime semantics: a pooled connection's server session outlives its
      //dispose, still owning the session-scoped lock as a ghost until the pool reuses or prunes it. Unpooled, dispose
      //really ends the session - and this one long-held connection per endpoint gains nothing from a pool anyway.
      NpgsqlConnection? connection = new(new NpgsqlConnectionStringBuilder(_connectionFactory.ConnectionString) { Pooling = false }.ConnectionString);
      try
      {
         await connection.OpenAsync().caf();
         var command = connection.CreateCommand();
         await using(command.caf())
         {
            command.CommandText = "SELECT pg_try_advisory_lock(@LockKey)";
            command.Parameters.AddWithValue("LockKey", ProcessLockKeys.Int64Key(connection.Database, endpointName));
            if(!(bool)(await command.ExecuteScalarAsync().caf())!) return null;

            //Stamped on the very session that now holds the lock, before the hold is handed back: the live lock and its
            //recorded holder become one fact for any process that later reads the row after being refused this lock.
            await RecordLockHolderOnTheLockSessionAsync(connection, endpointName, lockHolderDescription).caf();

            var session = new ProcessLockSession(connection, onLockLostWhileHeld);
            connection = null; //Ownership transferred: the session holds the lock by holding the connection.
            return session;
         }
      }
      finally
      {
         if(connection != null) await connection.DisposeAsync().caf();
      }
   }

   static async Task RecordLockHolderOnTheLockSessionAsync(NpgsqlConnection lockSession, string endpointName, string lockHolderDescription)
   {
      var command = lockSession.CreateCommand();
      await using(command.caf())
      {
         command.CommandText = $"UPDATE {Catalog.TableName} SET {Catalog.LockHolderDescription} = @holder WHERE {Catalog.EndpointName} = @name";
         command.Parameters.AddWithValue("holder", lockHolderDescription);
         command.Parameters.AddWithValue("name", endpointName);
         await command.ExecuteNonQueryAsync().caf();
      }
   }

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<NpgsqlCommand> addFilterParameter) =>
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
                              endpointId: new EndpointId(reader.GetGuid(1)),
                              lockHolderDescription: await reader.IsDBNullAsync(2).caf() ? null : reader.GetString(2)));
            }

            return entries;
         }).caf();

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
