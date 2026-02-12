using Document = Compze.Core.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Sql.PostgreSql.Private.DocumentDb;

public partial class PgSqlDocumentDbSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Document.TableName} 
       (
           {Document.Id}          VARCHAR(500)                NOT NULL,
           {Document.ValueTypeId} UUID                        NOT NULL,
           {Document.Created}     TIMESTAMP with time zone    NOT NULL,
           {Document.Updated}     TIMESTAMP with time zone    NOT NULL,
           {Document.Value}       TEXT                        NOT NULL,

           PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
       );


       """;
}
