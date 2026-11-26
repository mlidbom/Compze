using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.MySql._private;

partial class MySqlMemoryImageStateStoreSqlLayer
{
   internal static readonly string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Schema.TaggregateTable}
       (
           {Schema.Id}        CHAR(36)     NOT NULL,
           {Schema.ClassName} VARCHAR({Schema.ClassNameLength}) NOT NULL,
           {Schema.Snapshot}  LONGTEXT     NOT NULL,

           PRIMARY KEY ({Schema.Id})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

       """;
}
