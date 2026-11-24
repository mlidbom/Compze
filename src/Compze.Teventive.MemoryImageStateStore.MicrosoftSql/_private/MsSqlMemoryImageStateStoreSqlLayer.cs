using Compze.Abstractions;
using Compze.Sql.Common._internal;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Microsoft.Data.SqlClient;
using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.MicrosoftSql._private;

///<summary>A memory image state store's tables in SQL Server. A taggregate's lock is an application lock on its id, owned by the transaction:<br/>
/// unlike a row lock, it locks the id of a taggregate that has no row yet, and unlike a key range lock, it locks nothing else.</summary>
partial class MsSqlMemoryImageStateStoreSqlLayer : IMemoryImageStateStoreSqlLayer
{
   ///<summary>How long a transaction waits for another's lock of a taggregate before it fails, naming the taggregate.</summary>
   const int LockTimeoutMilliseconds = 30_000;

   readonly IMsSqlConnectionPool _connectionPool;
   readonly MsSqlSqlLayerSchemaManager _schemaManager;

   internal MsSqlMemoryImageStateStoreSqlLayer(IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager)
   {
      _connectionPool = connectionPool;
      _schemaManager = schemaManager;
   }

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
   static string ClassNameParameters(SqlCommand command, IReadOnlyList<string> classNames)
   {
      if(classNames.Count == 0) return "NULL";
      for(var index = 0; index < classNames.Count; index++) command.AddNVarcharParameter($"{Schema.ClassName}{index}", Schema.ClassNameLength, classNames[index]);
      return string.Join(", ", classNames.Select((_, index) => $"@{Schema.ClassName}{index}"));
   }

   ///<summary>Takes the application lock, failing if it can't be had within <see cref="LockTimeoutMilliseconds"/> or the transaction<br/>
   /// was chosen as a deadlock's victim, then reads the row in a statement of its own, which sees what the lock's last holder committed.</summary>
   public TaggregateRow? LockAndReadUnlessHeldAs(TaggregateId id, CommitNumber? heldAs)
   {
      _schemaManager.EnsureSchemaInitialized();
      var rows = _connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                               DECLARE @LockResult int;
                                                                               EXEC @LockResult = sp_getapplock @Resource = @LockResource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {LockTimeoutMilliseconds};
                                                                               IF @LockResult < 0 THROW 51000, @LockFailure, 1;
                                                                               SELECT {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber} FROM {Schema.TaggregateTable}
                                                                               WHERE {Schema.Id} = @{Schema.Id} AND {Schema.CommitNumber} <> @HeldAs;
                                                                               """)
                                                              .AddNVarcharParameter("LockResource", 255, $"Compze.MemoryImageStateStore:{id.Value}")
                                                              .AddNVarcharParameter("LockFailure", 2048, $"The memory image state store couldn't take the lock of taggregate {id.Value} within {LockTimeoutMilliseconds / 1000} seconds, or its transaction was chosen as a deadlock's victim: another unit of work, of this memory image or another, holds the taggregate.")
                                                              .AddParameter(Schema.Id, id.Value)
                                                              .AddParameter("HeldAs", heldAs?.Value ?? -1)
                                                              .ExecuteReaderAndSelect(reader => new TaggregateRow(id, reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), new CommitNumber(reader.GetInt64(2)))));
      return rows.SingleOrDefault();
   }

   public CommitNumber TakeTheNextCommitNumber() =>
      new((long)_connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                              UPDATE {Schema.CommitCounterTable} SET {Schema.LastCommitNumber} = {Schema.LastCommitNumber} + 1
                                                                              OUTPUT inserted.{Schema.LastCommitNumber}
                                                                              WHERE {Schema.Id} = {Schema.TheCounterRow};
                                                                              """)
                                                             .ExecuteScalar()!));

   ///<summary>Updates each row, and inserts one that isn't there: safe without a range lock, since the transaction holds each taggregate's<br/>
   /// application lock, or, writing every taggregate anew, the commit counter.</summary>
   public void Write(CommitNumber committedAs, IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted) =>
      _connectionPool.UseConnection(connection =>
      {
         foreach(var row in saved)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     UPDATE {Schema.TaggregateTable} SET {Schema.ClassName} = @{Schema.ClassName}, {Schema.Snapshot} = @{Schema.Snapshot}, {Schema.CommitNumber} = @{Schema.CommitNumber}
                                                                     WHERE {Schema.Id} = @{Schema.Id};
                                                                     IF @@ROWCOUNT = 0
                                                                        INSERT INTO {Schema.TaggregateTable} ({Schema.Id}, {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber})
                                                                        VALUES (@{Schema.Id}, @{Schema.ClassName}, @{Schema.Snapshot}, @{Schema.CommitNumber});
                                                                     """)
                                                    .AddParameter(Schema.Id, row.Id.Value)
                                                    .AddNVarcharParameter(Schema.ClassName, Schema.ClassNameLength, row.ClassName)
                                                    .AddNVarcharMaxParameter(Schema.Snapshot, row.Snapshot)
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
