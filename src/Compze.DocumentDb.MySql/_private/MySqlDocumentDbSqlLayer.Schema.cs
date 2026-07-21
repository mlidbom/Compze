using Document = Compze.DocumentDb._internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.DocumentDb.MySql._private;

partial class MySqlDocumentDbSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Document.TableName}
       (
       {Document.Id}          VARCHAR(500) NOT NULL,
       {Document.ValueTypeId} INT          NOT NULL,
       {Document.Created}     DATETIME     NOT NULL,
       {Document.Updated}     DATETIME     NOT NULL,
       {Document.Value}       MEDIUMTEXT   NOT NULL,

       PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;


       """;
}
