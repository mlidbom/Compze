using Compze.Abstractions;
using Compze.Sql.Common._internal;
using Compze.Sql.PostgreSql._internal;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using NpgsqlTypes;
using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.PostgreSql._private;

///<summary>A memory image state store's tables in PostgreSQL. A taggregate's lock is a transaction-level advisory lock on its id: in the<br/>
/// two-key form, with the store's own first key, so that it shares no lock with the tevent store's, which take the one-key form.</summary>
partial class PgSqlMemoryImageStateStoreSqlLayer : IMemoryImageStateStoreSqlLayer
{
   ///<summary>The first key of every taggregate's advisory lock: "MISS", for memory image state store.</summary>
   const int TheStoresAdvisoryLockKey = 0x4D49_5353;

   readonly IPgSqlConnectionPool _connectionPool;
   readonly PgSqlSqlLayerSchemaManager _schemaManager;

   internal PgSqlMemoryImageStateStoreSqlLayer(IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
   }

   public string DatabaseIdentity => _connectionPool.ConnectionString;

   public TaggregateRows ReadAll(IReadOnlyList<string> classNames) => ReadRows(classNames, committedAfter: null);

   public TaggregateRows ReadCommittedAfter(CommitNumber lastSeen, IReadOnlyList<string> classNames) => ReadRows(classNames, committedAfter: lastSeen);

   TaggregateRows ReadRows(IReadOnlyList<string> classNames, CommitNumber? committedAfter)
   {
      _schemaManager.EnsureSchemaInitialized();
      return _connectionPool.UseCommand(command =>
      {
         command.SetCommandText($"""
                                 SELECT {Schema.LastCommitNumber} FROM {Schema.CommitCounterTable} WHERE {Schema.Id} = {Schema.TheCounterRow};
                                 SELECT {Schema.Id}, {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber} FROM {Schema.TaggregateTable}
                                 WHERE {Schema.ClassName} = ANY(@ClassNames){(committedAfter == null ? "" : $" AND {Schema.CommitNumber} > @{Schema.CommitNumber}")};
                                 """)
                .AddParameter("ClassNames", NpgsqlDbType.Array | NpgsqlDbType.Varchar, classNames.ToArray());
         if(committedAfter is {} lastSeen) command.AddParameter(Schema.CommitNumber, lastSeen.Value);
         using var reader = command.ExecuteReader();
         reader.Read();
         var holdingEveryCommitThrough = new CommitNumber(reader.GetInt64(0));
         reader.NextResult();
         var rows = new List<TaggregateRow>();
         while(reader.Read()) rows.Add(new TaggregateRow(new TaggregateId(reader.GetGuid(0)), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), new CommitNumber(reader.GetInt64(3))));
         return new TaggregateRows(holdingEveryCommitThrough, rows);
      });
   }

   ///<summary>Takes the advisory lock, then reads the row in a statement of its own, which sees what the lock's last holder committed.</summary>
   public TaggregateRow? LockAndReadUnlessHeldAs(TaggregateId id, CommitNumber? heldAs)
   {
      _schemaManager.EnsureSchemaInitialized();
      return _connectionPool.UseCommand(command =>
      {
         command.SetCommandText($"""
                                 SELECT pg_advisory_xact_lock({TheStoresAdvisoryLockKey}, @LockKey);
                                 SELECT {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber} FROM {Schema.TaggregateTable}
                                 WHERE {Schema.Id} = @{Schema.Id} AND {Schema.CommitNumber} <> @HeldAs;
                                 """)
                .AddParameter("LockKey", BitConverter.ToInt32(id.Value.ToByteArray(), startIndex: 0))
                .AddParameter(Schema.Id, id.Value)
                .AddParameter("HeldAs", heldAs?.Value ?? -1);
         using var reader = command.ExecuteReader();
         reader.NextResult();
         return reader.Read() ? new TaggregateRow(id, reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), new CommitNumber(reader.GetInt64(2))) : null;
      });
   }

   public CommitNumber TakeTheNextCommitNumber() =>
      new((long)_connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                              UPDATE {Schema.CommitCounterTable} SET {Schema.LastCommitNumber} = {Schema.LastCommitNumber} + 1
                                                                              WHERE {Schema.Id} = {Schema.TheCounterRow}
                                                                              RETURNING {Schema.LastCommitNumber};
                                                                              """)
                                                             .ExecuteScalar()!));

   public void Write(CommitNumber committedAs, IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted) =>
      _connectionPool.UseConnection(connection =>
      {
         foreach(var row in saved)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     INSERT INTO {Schema.TaggregateTable} ({Schema.Id}, {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber})
                                                                     VALUES (@{Schema.Id}, @{Schema.ClassName}, @{Schema.Snapshot}, @{Schema.CommitNumber})
                                                                     ON CONFLICT ({Schema.Id}) DO UPDATE SET {Schema.ClassName} = EXCLUDED.{Schema.ClassName},
                                                                                                             {Schema.Snapshot} = EXCLUDED.{Schema.Snapshot},
                                                                                                             {Schema.CommitNumber} = EXCLUDED.{Schema.CommitNumber};
                                                                     """)
                                                    .AddParameter(Schema.Id, row.Id.Value)
                                                    .AddVarcharParameter(Schema.ClassName, Schema.ClassNameLength, row.ClassName)
                                                    .AddMediumTextParameter(Schema.Snapshot, row.Snapshot)
                                                    .AddParameter(Schema.CommitNumber, committedAs.Value)
                                                    .ExecuteNonQuery());
         foreach(var id in deleted)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     UPDATE {Schema.TaggregateTable} SET {Schema.Snapshot} = NULL, {Schema.CommitNumber} = @{Schema.CommitNumber}
                                                                     WHERE {Schema.Id} = @{Schema.Id};
                                                                     """)
                                                    .AddParameter(Schema.Id, id.Value)
                                                    .AddParameter(Schema.CommitNumber, committedAs.Value)
                                                    .ExecuteNonQuery());
      });
}
