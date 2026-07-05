using T = Compze.ServiceBus.Transport.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlInboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public const string SchemaCreationSql =
      $"""

           CREATE TABLE IF NOT EXISTS {T.TableName}
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

               UNIQUE INDEX IX_{T.TableName}_Unique_{T.TessageId} ( {T.TessageId} )
           )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;


       """;
}
