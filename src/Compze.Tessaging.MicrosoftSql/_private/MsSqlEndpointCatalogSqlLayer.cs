using System.Data;
using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Microsoft.Data.SqlClient;
using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql._private;

partial class MsSqlEndpointCatalogSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddNVarcharMaxParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddParameter("filter", endpointId.Value)).caf()).SingleOrDefault();

   public async Task<bool> TryInsertEntryAsync(string endpointName, EndpointId endpointId, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //UPDLOCK+HOLDLOCK makes the existence check and the insert one serialized act, so a racing
                       //registration cannot slip between them - the loser sees the row and inserts nothing.
                       $"""

                        INSERT INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc})
                             SELECT @{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc}
                        WHERE NOT EXISTS (SELECT 1 FROM {Catalog.TableName} WITH (UPDLOCK, HOLDLOCK) WHERE {Catalog.EndpointName} = @{Catalog.EndpointName})

                        """)
                   .AddNVarcharMaxParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.EndpointId, endpointId.Value)
                   .AddDateTime2Parameter(Catalog.CreatedUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   //The lock is a session-owned sp_getapplock on a dedicated connection: it lives exactly as long as the session, so a
   //crashed process's lock is released when the server notices its connection die, and no pause can lose a live holder's
   //lock. sp_getapplock resources are scoped to the current database, so the endpoint name alone identifies the lock.
   public async Task<ITessagingSqlLayer.IEndpointProcessLockHold?> TryTakeProcessLockAsync(string endpointName, string lockHolderDescription, Action<Exception> onLockLostWhileHeld)
   {
      //Pooling would break the lock's session-lifetime semantics: a pooled connection's server session outlives its
      //dispose, still owning the session-scoped lock as a ghost until the pool reuses or prunes it. Unpooled, dispose
      //really ends the session - and this one long-held connection per endpoint gains nothing from a pool anyway.
      SqlConnection? connection = new(new SqlConnectionStringBuilder(_connectionFactory.ConnectionString) { Pooling = false }.ConnectionString);
      try
      {
         await connection.OpenAsync().caf();
         var command = connection.CreateCommand();
         await using(command.caf())
         {
            command.CommandText = "sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("Resource", $"CompzeEndpointProcessLock_{endpointName}");
            command.Parameters.AddWithValue("LockMode", "Exclusive");
            command.Parameters.AddWithValue("LockOwner", "Session");
            command.Parameters.AddWithValue("LockTimeout", 0);
            var lockResult = command.Parameters.Add(new SqlParameter("ReturnValue", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            await command.ExecuteNonQueryAsync().caf();
            //0 = granted, 1 = granted after waiting; negative values are refusals and failures.
            if((int)lockResult.Value < 0) return null;

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

   static async Task RecordLockHolderOnTheLockSessionAsync(SqlConnection lockSession, string endpointName, string lockHolderDescription)
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

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<SqlCommand> addFilterParameter) =>
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
