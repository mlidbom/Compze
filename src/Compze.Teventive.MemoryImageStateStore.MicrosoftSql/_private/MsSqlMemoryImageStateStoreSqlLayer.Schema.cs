using Schema = Compze.Teventive.MemoryImageStateStore._internal.SqlLayer.IMemoryImageStateStoreSqlLayer.TableSchemaStrings;

namespace Compze.Teventive.MemoryImageStateStore.MicrosoftSql._private;

partial class MsSqlMemoryImageStateStoreSqlLayer
{
   internal static readonly string SchemaCreationSql =
      $"""

       IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{Schema.TaggregateTable}')
       BEGIN
           CREATE TABLE {Schema.TaggregateTable}
           (
               {Schema.Id}           uniqueidentifier NOT NULL,
               {Schema.ClassName}    nvarchar({Schema.ClassNameLength}) NOT NULL,
               {Schema.Snapshot}     nvarchar(max)    NULL,
               {Schema.CommitNumber} bigint           NOT NULL,

               CONSTRAINT PK_{Schema.TaggregateTable} PRIMARY KEY CLUSTERED ({Schema.Id})
           );

           CREATE INDEX IX_{Schema.TaggregateTable}_{Schema.CommitNumber} ON {Schema.TaggregateTable} ({Schema.CommitNumber});
       END

       IF NOT EXISTS(SELECT NAME FROM sys.tables WHERE name = '{Schema.CommitCounterTable}')
       BEGIN
           CREATE TABLE {Schema.CommitCounterTable}
           (
               {Schema.Id}               int    NOT NULL CONSTRAINT PK_{Schema.CommitCounterTable} PRIMARY KEY CHECK ({Schema.Id} = {Schema.TheCounterRow}),
               {Schema.LastCommitNumber} bigint NOT NULL
           );

           INSERT INTO {Schema.CommitCounterTable} ({Schema.Id}, {Schema.LastCommitNumber}) VALUES ({Schema.TheCounterRow}, 0);
       END

       """;
}
