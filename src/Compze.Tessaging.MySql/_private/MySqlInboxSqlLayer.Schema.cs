using Compze.Tessaging._internal.SqlLayer;
using T = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlInboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

           CREATE TABLE IF NOT EXISTS {tables.InboxTessages}
           (
               {T.GeneratedId}         bigint          NOT NULL  AUTO_INCREMENT,
               {T.TypeId}              int             NOT NULL,
               {T.TessageId}           {MySqlGuidType} NOT NULL,
               {T.Status}              smallint        NOT NULL,
               {T.Body}                mediumtext      NOT NULL,
               {T.ExceptionCount}      int             NOT NULL  DEFAULT 0,
               {T.ExceptionType}       varchar(500)    NULL,
               {T.ExceptionStackTrace} mediumtext      NULL,
               {T.ExceptionTessage}    mediumtext      NULL,


               PRIMARY KEY ( {T.GeneratedId} ),

               UNIQUE INDEX IX_{tables.InboxTessages}_Unique_{T.TessageId} ( {T.TessageId} )
           )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;


       """;
}
