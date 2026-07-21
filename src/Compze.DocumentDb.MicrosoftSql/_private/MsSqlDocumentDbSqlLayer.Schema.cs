using Document = Compze.DocumentDb._internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.DocumentDb.MicrosoftSql._private;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class MsSqlDocumentDbSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{Document.TableName}')
       BEGIN 
           CREATE TABLE {Document.TableName}
           (
               {Document.Id}          nvarchar(500)    NOT NULL,
               {Document.ValueTypeId} int              NOT NULL,
               {Document.Created}     datetime2        NOT NULL,
               {Document.Updated}     datetime2        NOT NULL,
               {Document.Value}       nvarchar(max)    NOT NULL,
                  
               CONSTRAINT PK_{Document.TableName} PRIMARY KEY CLUSTERED 
                  ({Document.Id} ASC, {Document.ValueTypeId} ASC)
                  WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF)
           )

       END

       """;
}
