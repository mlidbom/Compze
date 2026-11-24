using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.PostgreSql._private;

partial class PgSqlMemoryImageStateStoreSqlLayer
{
   ///<summary>The statements that create the store's tables, each run as a command of its own.</summary>
   internal static readonly IReadOnlyList<string> SchemaCreationStatements =
   [
      $"""
       CREATE TABLE IF NOT EXISTS {Schema.TaggregateTable}
       (
           {Schema.Id}           UUID         NOT NULL PRIMARY KEY,
           {Schema.ClassName}    VARCHAR({Schema.ClassNameLength}) NOT NULL,
           {Schema.Snapshot}     TEXT         NULL,
           {Schema.CommitNumber} BIGINT       NOT NULL
       )
       """,
      $"CREATE INDEX IF NOT EXISTS IX_{Schema.TaggregateTable}_{Schema.CommitNumber} ON {Schema.TaggregateTable} ({Schema.CommitNumber})",
      $"""
       CREATE TABLE IF NOT EXISTS {Schema.CommitCounterTable}
       (
           {Schema.Id}               INTEGER NOT NULL PRIMARY KEY CHECK ({Schema.Id} = {Schema.TheCounterRow}),
           {Schema.LastCommitNumber} BIGINT  NOT NULL
       )
       """,
      $"INSERT INTO {Schema.CommitCounterTable} ({Schema.Id}, {Schema.LastCommitNumber}) VALUES ({Schema.TheCounterRow}, 0) ON CONFLICT ({Schema.Id}) DO NOTHING"
   ];
}
