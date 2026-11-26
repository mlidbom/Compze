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
           {Schema.Id}        UUID         NOT NULL PRIMARY KEY,
           {Schema.ClassName} VARCHAR({Schema.ClassNameLength}) NOT NULL,
           {Schema.Snapshot}  TEXT         NOT NULL
       )
       """
   ];
}
