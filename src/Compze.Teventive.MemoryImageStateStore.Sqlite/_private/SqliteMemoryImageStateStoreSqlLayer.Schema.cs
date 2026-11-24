using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.Sqlite._private;

partial class SqliteMemoryImageStateStoreSqlLayer
{
   internal static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TaggregateTable}
       (
           {Schema.Id}           TEXT    NOT NULL PRIMARY KEY,
           {Schema.ClassName}    TEXT    NOT NULL,
           {Schema.Snapshot}     TEXT    NULL,
           {Schema.CommitNumber} INTEGER NOT NULL
       );

       CREATE INDEX IF NOT EXISTS IX_{Schema.TaggregateTable}_{Schema.CommitNumber}
           ON {Schema.TaggregateTable} ({Schema.CommitNumber});

       CREATE TABLE IF NOT EXISTS {Schema.CommitCounterTable}
       (
           {Schema.Id}               INTEGER NOT NULL PRIMARY KEY CHECK ({Schema.Id} = {Schema.TheCounterRow}),
           {Schema.LastCommitNumber} INTEGER NOT NULL
       );

       INSERT OR IGNORE INTO {Schema.CommitCounterTable} ({Schema.Id}, {Schema.LastCommitNumber}) VALUES ({Schema.TheCounterRow}, 0);

       """;
}
