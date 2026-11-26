using Compze.Abstractions;
using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.ProcessLocks;
using Compze.Sql.Sqlite._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Threading;
using Microsoft.Data.Sqlite;
using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.Sqlite._private;

///<summary>A memory image state store's tables in SQLite.</summary>
partial class SqliteMemoryImageStateStoreSqlLayer : IMemoryImageStateStoreSqlLayer
{
   readonly ISqliteConnectionPool _connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager;
   readonly IDatabaseProcessLocks _processLocks;

   internal SqliteMemoryImageStateStoreSqlLayer(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager, IDatabaseProcessLocks processLocks)
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
         command.SetCommandText($"SELECT {Schema.Id}, {Schema.ClassName}, {Schema.Snapshot} FROM {Schema.TaggregateTable} WHERE {Schema.ClassName} IN ({ClassNameParameters(command, classNames)});");
         using var reader = command.ExecuteReader();
         var rows = new List<TaggregateRow>();
         while(reader.Read()) rows.Add(new TaggregateRow(new TaggregateId(reader.GetGuidFromString(0)), reader.GetString(1), reader.GetString(2)));
         return rows;
      });
   }

   ///<summary>A parameter of <paramref name="command"/> for each of <paramref name="classNames"/>, as a list for an IN clause; NULL, which<br/>
   /// matches nothing, for none.</summary>
   static string ClassNameParameters(SqliteCommand command, IReadOnlyList<string> classNames)
   {
      if(classNames.Count == 0) return "NULL";
      for(var index = 0; index < classNames.Count; index++) command.AddMediumTextParameter($"{Schema.ClassName}{index}", classNames[index]);
      return string.Join(", ", classNames.Select((_, index) => $"@{Schema.ClassName}{index}"));
   }

   public void Write(IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted) =>
      _connectionPool.UseConnection(connection =>
      {
         foreach(var row in saved)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     INSERT INTO {Schema.TaggregateTable} ({Schema.Id}, {Schema.ClassName}, {Schema.Snapshot})
                                                                     VALUES (@{Schema.Id}, @{Schema.ClassName}, @{Schema.Snapshot})
                                                                     ON CONFLICT ({Schema.Id}) DO UPDATE SET {Schema.ClassName} = excluded.{Schema.ClassName},
                                                                                                             {Schema.Snapshot} = excluded.{Schema.Snapshot};
                                                                     """)
                                                    .AddMediumTextParameter(Schema.Id, row.Id.Value.ToString())
                                                    .AddMediumTextParameter(Schema.ClassName, row.ClassName)
                                                    .AddMediumTextParameter(Schema.Snapshot, row.Snapshot)
                                                    .ExecuteNonQuery());
         foreach(var id in deleted)
            connection.UseCommand(command => command.SetCommandText($"DELETE FROM {Schema.TaggregateTable} WHERE {Schema.Id} = @{Schema.Id};")
                                                    .AddMediumTextParameter(Schema.Id, id.Value.ToString())
                                                    .ExecuteNonQuery());
      });
}
