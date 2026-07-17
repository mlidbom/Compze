using Tessage = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlInboxSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{Tessage.TableName}')
       BEGIN
           CREATE TABLE {Tessage.TableName}
           (
               {Tessage.GeneratedId}         bigint IDENTITY(1,1) NOT NULL,
               {Tessage.TypeId}              int                  NOT NULL,
               {Tessage.TessageId}           uniqueidentifier     NOT NULL,
               {Tessage.Status}              smallint             NOT NULL,
               {Tessage.Body}                nvarchar(MAX)        NOT NULL,
               {Tessage.ExceptionCount}      int                  NOT NULL  DEFAULT 0,
               {Tessage.ExceptionType}       nvarchar(500)        NULL,
               {Tessage.ExceptionStackTrace} nvarchar(MAX)        NULL,
               {Tessage.ExceptionTessage}    nvarchar(MAX)        NULL,


               CONSTRAINT PK_{Tessage.TableName} PRIMARY KEY CLUSTERED ( [{Tessage.GeneratedId}] ASC ),

               CONSTRAINT IX_{Tessage.TableName}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
           )
       END

       """;
}
