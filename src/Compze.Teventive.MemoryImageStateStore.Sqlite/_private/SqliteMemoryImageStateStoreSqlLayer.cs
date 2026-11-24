using Compze.Abstractions;
using Compze.Sql.Common._internal;
using Compze.Sql.Sqlite._internal;
using Compze.Teventive.MemoryImageStateStore._internal;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Microsoft.Data.Sqlite;
using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.Sqlite._private;

///<summary>A memory image state store's tables in SQLite. A transaction holds the database's write lock from its start, behind Compze's gate<br/>
/// for each database in each process and SQLite's BEGIN IMMEDIATE across processes, so a taggregate needs no lock of its own.</summary>
partial class SqliteMemoryImageStateStoreSqlLayer : IMemoryImageStateStoreSqlLayer
{
   readonly ISqliteConnectionPool _connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager;

   internal SqliteMemoryImageStateStoreSqlLayer(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager)
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
         while(reader.Read()) rows.Add(new TaggregateRow(new TaggregateId(reader.GetGuidFromString(0)), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), new CommitNumber(reader.GetInt64(3))));
         return new TaggregateRows(holdingEveryCommitThrough, rows);
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

   ///<summary>Reads the row inside the transaction, which holds the database's write lock already.</summary>
   public TaggregateRow? LockAndReadUnlessHeldAs(TaggregateId id, CommitNumber? heldAs)
   {
      _schemaManager.EnsureSchemaInitialized();
      var rows = _connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                               SELECT {Schema.ClassName}, {Schema.Snapshot}, {Schema.CommitNumber} FROM {Schema.TaggregateTable}
                                                                               WHERE {Schema.Id} = @{Schema.Id} AND {Schema.CommitNumber} <> @HeldAs;
                                                                               """)
                                                              .AddMediumTextParameter(Schema.Id, id.Value.ToString())
                                                              .AddParameter("HeldAs", heldAs?.Value ?? -1)
                                                              .ExecuteReaderAndSelect(reader => new TaggregateRow(id, reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), new CommitNumber(reader.GetInt64(2)))));
      return rows.SingleOrDefault();
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
                                                                     ON CONFLICT ({Schema.Id}) DO UPDATE SET {Schema.ClassName} = excluded.{Schema.ClassName},
                                                                                                             {Schema.Snapshot} = excluded.{Schema.Snapshot},
                                                                                                             {Schema.CommitNumber} = excluded.{Schema.CommitNumber};
                                                                     """)
                                                    .AddMediumTextParameter(Schema.Id, row.Id.Value.ToString())
                                                    .AddMediumTextParameter(Schema.ClassName, row.ClassName)
                                                    .AddMediumTextParameter(Schema.Snapshot, row.Snapshot)
                                                    .AddParameter(Schema.CommitNumber, committedAs.Value)
                                                    .ExecuteNonQuery());
         foreach(var id in deleted)
            connection.UseCommand(command => command.SetCommandText($"""
                                                                     UPDATE {Schema.TaggregateTable} SET {Schema.Snapshot} = NULL, {Schema.CommitNumber} = @{Schema.CommitNumber}
                                                                     WHERE {Schema.Id} = @{Schema.Id};
                                                                     """)
                                                    .AddMediumTextParameter(Schema.Id, id.Value.ToString())
                                                    .AddParameter(Schema.CommitNumber, committedAs.Value)
                                                    .ExecuteNonQuery());
      });
}
