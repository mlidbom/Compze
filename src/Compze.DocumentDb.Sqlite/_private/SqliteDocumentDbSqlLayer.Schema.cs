using Document = Compze.DocumentDb._internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.DocumentDb.Sqlite._private;

partial class SqliteDocumentDbSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Document.TableName} 
       (
           {Document.Id}          TEXT    NOT NULL,
           {Document.ValueTypeId} INTEGER NOT NULL,
           {Document.Created}     INTEGER NOT NULL,
           {Document.Updated}     INTEGER NOT NULL,
           {Document.Value}       TEXT    NOT NULL,
              
           PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
       );

       CREATE INDEX IF NOT EXISTS IX_{Document.TableName}_{Document.ValueTypeId} 
           ON {Document.TableName} ({Document.ValueTypeId});

       """;
}
