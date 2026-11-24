using Document = Compze.DocumentDb._internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.DocumentDb.PostgreSql._private;

partial class PgSqlDocumentDbSqlLayer
{
   public static readonly IReadOnlyList<string> SchemaCreationStatements =
   [
      $"""
       CREATE TABLE IF NOT EXISTS {Document.TableName}
       (
           {Document.Id}          VARCHAR(500)                NOT NULL,
           {Document.ValueTypeId} INT                         NOT NULL,
           {Document.Created}     TIMESTAMP with time zone    NOT NULL,
           {Document.Updated}     TIMESTAMP with time zone    NOT NULL,
           {Document.Value}       TEXT                        NOT NULL,

           PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
       )
       """
   ];
}
