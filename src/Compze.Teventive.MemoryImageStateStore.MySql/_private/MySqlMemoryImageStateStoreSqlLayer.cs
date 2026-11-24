using System.Globalization;
using Compze.Abstractions;
using Compze.Sql.Common._internal;
using Compze.Sql.MySql._internal;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using MySqlConnector;
using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.MySql._private;

///<summary>A memory image state store's tables in MySQL. A taggregate's lock is its id's row in a lock table of its own, inserted if it has<br/>
/// none: InnoDB holds a row's lock until the transaction commits or rolls back, at any isolation level, and at read committed it locks<br/>
/// no gap, so a lock on the taggregate's own row would lock nothing for a taggregate that has none yet.</summary>
partial class MySqlMemoryImageStateStoreSqlLayer : IMemoryImageStateStoreSqlLayer
{
   readonly IMySqlConnectionPool _connectionPool;
   readonly MySqlSqlLayerSchemaManager _schemaManager;

   internal MySqlMemoryImageStateStoreSqlLayer(IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager)
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
                                 WHERE {Schema.ClassName} IN ({ClassNameParameters(command, classNames)}){(committedAfter == null ? "" : $" AND {Schema.CommitNumber} > @{Schema.CommitNumber}")};
                                 """);
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

   ///<summary>A parameter of <paramref name="command"/> for each of <paramref name="classNames"/>, as a list for an IN clause; NULL, which<br/>
   /// matches nothing, for none.</summary>
   static string ClassNameParameters(MySqlCommand command, IReadOnlyList<string> classNames)
   {
      if(classNames.Count == 0) return "NULL";
      for(var index = 0; index < classNames.Count; index++) command.AddVarcharParameter($"{Schema.ClassName}{index}", Schema.ClassNameLength, classNames[index]);
      return string.Join(", ", classNames.Select((_, index) => $"@{Schema.ClassName}{index}"));
   }

   ///<summary>Takes the lock row, then reads the taggregate's row in a statement of its own, which at read committed sees what the<br/>
   /// lock's last holder committed.</summary>
   public TaggregateRow? LockAndReadUnlessHeldAs(TaggregateId id, CommitNumber? heldAs)
   {
      _schemaManager.EnsureSchemaInitialized();
      return _connectionPool.UseCommand(command =>
      {
         command.SetCommandText($"""
                                 INSERT INTO {MySqlMemoryImageStateStoreSqlLayer.TaggregateLockTable} ({Schema.Id}) VALUES (@{Schema.Id}) ON DUPLICATE KEY UPDATE {Schema.Id} = {Schema.Id};
                                 SELECT {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber} FROM {Schema.TaggregateTable}
                                 WHERE {Schema.Id} = @{Schema.Id} AND {Schema.CommitNumber} <> @HeldAs;
                                 """)
                .AddParameter(Schema.Id, id.Value)
                .AddParameter("HeldAs", heldAs?.Value ?? -1);
         using var reader = command.ExecuteReader();
         return reader.Read() ? new TaggregateRow(id, reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), new CommitNumber(reader.GetInt64(2))) : null;
      });
   }

   ///<summary>MySQL's UPDATE returns no row, so the number is read back in the same transaction, which sees its own update.</summary>
   public CommitNumber TakeTheNextCommitNumber()
   {
      var lastCommitNumber = _connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                                         UPDATE {Schema.CommitCounterTable} SET {Schema.LastCommitNumber} = {Schema.LastCommitNumber} + 1
                                                                                         WHERE {Schema.Id} = {Schema.TheCounterRow};
                                                                                         SELECT {Schema.LastCommitNumber} FROM {Schema.CommitCounterTable} WHERE {Schema.Id} = {Schema.TheCounterRow};
                                                                                         """)
                                                                        .ExecuteScalar());
      return new CommitNumber(Convert.ToInt64(lastCommitNumber, CultureInfo.InvariantCulture));
   }

   public void Write(CommitNumber committedAs, IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted) =>
      _connectionPool.UseConnection(connection =>
      {
         foreach(var row in saved)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     INSERT INTO {Schema.TaggregateTable} ({Schema.Id}, {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber})
                                                                     VALUES (@{Schema.Id}, @{Schema.ClassName}, @{Schema.Snapshot}, @{Schema.CommitNumber}) AS Written
                                                                     ON DUPLICATE KEY UPDATE {Schema.ClassName} = Written.{Schema.ClassName},
                                                                                             {Schema.Snapshot} = Written.{Schema.Snapshot},
                                                                                             {Schema.CommitNumber} = Written.{Schema.CommitNumber};
                                                                     """)
                                                    .AddParameter(Schema.Id, row.Id.Value)
                                                    .AddVarcharParameter(Schema.ClassName, Schema.ClassNameLength, row.ClassName)
                                                    .AddParameter(Schema.Snapshot, MySqlDbType.LongText, row.Snapshot)
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
