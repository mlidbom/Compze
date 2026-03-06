using Document = Compze.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Internals.Sql.MySql.Private.DocumentDb;

partial class MySqlDocumentDbSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Document.TableName}
       (
       {Document.Id}          VARCHAR(500) NOT NULL,
       {Document.ValueTypeId} CHAR(38)     NOT NULL,
       {Document.Created}     DATETIME     NOT NULL,
       {Document.Updated}     DATETIME     NOT NULL,
       {Document.Value}       MEDIUMTEXT   NOT NULL,

       PRIMARY KEY ({Document.Id}, {Document.ValueTypeId})
       )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;


       """;
}
