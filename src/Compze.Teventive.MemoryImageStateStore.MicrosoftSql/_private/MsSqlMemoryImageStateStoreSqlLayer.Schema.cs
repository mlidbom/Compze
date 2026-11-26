using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.MicrosoftSql._private;

partial class MsSqlMemoryImageStateStoreSqlLayer
{
   internal static readonly string SchemaCreationSql =
      $"""

       IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{Schema.TaggregateTable}')
           CREATE TABLE {Schema.TaggregateTable}
           (
               {Schema.Id}        uniqueidentifier NOT NULL,
               {Schema.ClassName} nvarchar({Schema.ClassNameLength}) NOT NULL,
               {Schema.Snapshot}  nvarchar(max)    NOT NULL,

               CONSTRAINT PK_{Schema.TaggregateTable} PRIMARY KEY CLUSTERED ({Schema.Id})
           );

       """;
}
