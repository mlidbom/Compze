using Compze.Abstractions;
using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.PostgreSql._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Threading;
using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.PostgreSql._private;

///<summary>A memory image state store's tables in PostgreSQL.</summary>
///<remarks>Each command holds one statement. Npgsql 9 sends the second and later statements of a synchronous command from the thread<br/>
/// pool, and flushes the whole command only then, while the calling thread waits for the answer: with every pool thread busy, as<br/>
/// under many units of work at once, the server receives nothing until a pool thread comes free, and the command waits for seconds,<br/>
/// as long as the units of work's own 30-second timeouts. Found by the specification of units of work adding cards to one deck from<br/>
/// many threads at once (2026-11-24).</remarks>
partial class PgSqlMemoryImageStateStoreSqlLayer : IMemoryImageStateStoreSqlLayer
{
   readonly IPgSqlConnectionPool _connectionPool;
   readonly PgSqlSqlLayerSchemaManager _schemaManager;
   readonly IDatabaseProcessLocks _processLocks;

   internal PgSqlMemoryImageStateStoreSqlLayer(IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager, IDatabaseProcessLocks processLocks)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
      _processLocks = processLocks;
   }

   public IDisposable? TryTakeProcessLock(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld) =>
      _processLocks.TryTake(lockName, purpose, waitTimeout, onLostWhileHeld);

   public string? ProcessLockHolder(string lockName) => _processLocks.HolderOf(lockName);

   public IReadOnlyList<TaggregateRow> ReadAll(IReadOnlyList<string> classNames)
   {
      _schemaManager.EnsureSchemaInitialized();
      return _connectionPool.UseCommand(command =>
      {
         command.SetCommandText($"SELECT {Schema.Id}, {Schema.ClassName}, {Schema.Snapshot} FROM {Schema.TaggregateTable} WHERE {Schema.ClassName} = ANY(@ClassNames);")
                .Parameters.AddWithValue("ClassNames", classNames.ToArray()); //A string array, which Npgsql sends as text[].
         using var reader = command.ExecuteReader();
         var rows = new List<TaggregateRow>();
         while(reader.Read()) rows.Add(new TaggregateRow(new TaggregateId(reader.GetGuid(0)), reader.GetString(1), reader.GetString(2)));
         return rows;
      });
   }

   public void Write(IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted) =>
      _connectionPool.UseConnection(connection =>
      {
         foreach(var row in saved)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     INSERT INTO {Schema.TaggregateTable} ({Schema.Id}, {Schema.ClassName}, {Schema.Snapshot})
                                                                     VALUES (@{Schema.Id}, @{Schema.ClassName}, @{Schema.Snapshot})
                                                                     ON CONFLICT ({Schema.Id}) DO UPDATE SET {Schema.ClassName} = EXCLUDED.{Schema.ClassName},
                                                                                                             {Schema.Snapshot} = EXCLUDED.{Schema.Snapshot};
                                                                     """)
                                                    .AddParameter(Schema.Id, row.Id.Value)
                                                    .AddVarcharParameter(Schema.ClassName, Schema.ClassNameLength, row.ClassName)
                                                    .AddMediumTextParameter(Schema.Snapshot, row.Snapshot)
                                                    .ExecuteNonQuery());
         foreach(var id in deleted)
            connection.UseCommand(command => command.SetCommandText($"DELETE FROM {Schema.TaggregateTable} WHERE {Schema.Id} = @{Schema.Id};")
                                                    .AddParameter(Schema.Id, id.Value)
                                                    .ExecuteNonQuery());
      });
}
