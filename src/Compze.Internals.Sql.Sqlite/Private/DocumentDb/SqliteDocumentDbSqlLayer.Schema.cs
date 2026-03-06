using Document = Compze.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.DocumentDb;

partial class SqliteDocumentDbSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Document.TableName} 
       (
           {Document.Id}          TEXT    NOT NULL,
           {Document.ValueTypeId} TEXT    NOT NULL,
           {Document.Created}     INTEGER NOT NULL,
           {Document.Updated}     INTEGER NOT NULL,
           {Document.Value}       TEXT    NOT NULL,
              
           PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
       );

       CREATE INDEX IF NOT EXISTS IX_{Document.TableName}_{Document.ValueTypeId} 
           ON {Document.TableName} ({Document.ValueTypeId});

       """;
}
