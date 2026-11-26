using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.Sqlite._private;

partial class SqliteMemoryImageStateStoreSqlLayer
{
   internal const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TaggregateTable}
       (
           {Schema.Id}        TEXT NOT NULL PRIMARY KEY,
           {Schema.ClassName} TEXT NOT NULL,
           {Schema.Snapshot}  TEXT NOT NULL
       );

       """;
}
