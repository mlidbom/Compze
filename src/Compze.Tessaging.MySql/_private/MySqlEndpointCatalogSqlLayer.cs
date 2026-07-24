using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.MySql._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using MySqlConnector;
using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlEndpointCatalogSqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;

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

   //The lock is a session-scoped GET_LOCK on a dedicated connection: it lives exactly as long as the session, so a
   //crashed process's lock is released when the server notices its connection die, and no pause can lose a live holder's
   //lock. GET_LOCK names are server-wide and capped at 64 characters; the database name joins the hashed lock name so
   //identical endpoint names in different domain databases on one server can never collide.
   public async Task<ITessagingSqlLayer.IEndpointProcessLockHold?> TryTakeProcessLockAsync(string endpointName, string lockHolderDescription, Action<Exception> onLockLostWhileHeld)
   {
      //Pooling would break the lock's session-lifetime semantics: a pooled connection's server session outlives its
      //dispose, still owning the session-scoped lock as a ghost until the pool reuses or prunes it. Unpooled, dispose
      //really ends the session - and this one long-held connection per endpoint gains nothing from a pool anyway.
      MySqlConnection? connection = new(new MySqlConnectionStringBuilder(_connectionFactory.ConnectionString) { Pooling = false }.ConnectionString);
      try
      {
         await connection.OpenAsync().caf();
         var command = connection.CreateCommand();
         await using(command.caf())
         {
            command.CommandText = "SELECT GET_LOCK(@LockName, 0)";
            command.Parameters.AddWithValue("LockName", ProcessLockKeys.Name(connection.Database, endpointName));
            var lockResult = await command.ExecuteScalarAsync().caf();
            if(lockResult is 1L)
            {
               //Stamped on the very session that now holds the lock, before the hold is handed back: the live lock and its
               //recorded holder become one fact for any process that later reads the row after being refused this lock.
               await RecordLockHolderOnTheLockSessionAsync(connection, endpointName, lockHolderDescription).caf();

               var session = new ProcessLockSession(connection, onLockLostWhileHeld);
               connection = null; //Ownership transferred: the session holds the lock by holding the connection.
               return session;
            }

            //0 is the one result that means "held by another session"; NULL means the server failed to even try - a
            //refusal message naming another process would lie, so fail as what it is.
            if(lockResult is not 0L) throw new InvalidOperationException($"GET_LOCK failed instead of answering: returned {lockResult ?? "NULL"}.");
         }

         return null;
      }
      finally
      {
         if(connection != null) await connection.DisposeAsync().caf();
      }
   }

   static async Task RecordLockHolderOnTheLockSessionAsync(MySqlConnection lockSession, string endpointName, string lockHolderDescription)
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

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<MySqlCommand> addFilterParameter) =>
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
