using Compze.Tessaging.Transport.SqlLayer;
using Tessage = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlInboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{tables.InboxTessages}')
       BEGIN
           CREATE TABLE {tables.InboxTessages}
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


               CONSTRAINT PK_{tables.InboxTessages} PRIMARY KEY CLUSTERED ( [{Tessage.GeneratedId}] ASC ),

               CONSTRAINT IX_{tables.InboxTessages}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
           )
       END

       """;
}
