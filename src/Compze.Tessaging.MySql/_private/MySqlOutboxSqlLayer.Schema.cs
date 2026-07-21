using Compze.Tessaging._internal.SqlLayer;
using M = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using D = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlOutboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

        CREATE TABLE IF NOT EXISTS {tables.OutboxTessages}
        (
            {M.GeneratedId}       bigint          NOT NULL  AUTO_INCREMENT,
            {M.TypeId}            int             NOT NULL,
            {M.TessageId}         {MySqlGuidType} NOT NULL,
            {M.SerializedTessage} MEDIUMTEXT      NOT NULL,

            PRIMARY KEY ( {M.GeneratedId}),

            UNIQUE INDEX IX_{tables.OutboxTessages}_Unique_{M.TessageId} ( {M.TessageId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

        CREATE TABLE  IF NOT EXISTS {tables.OutboxTessageDispatching}
        (
            {D.TessageId}        {MySqlGuidType} NOT NULL,
            {D.EndpointId}       {MySqlGuidType} NOT NULL,
            {D.IsReceived}       bit             NOT NULL,
            {D.IsStranded}       bit             NOT NULL DEFAULT 0,
            {D.RetryCount}       int             NOT NULL DEFAULT 0,
            {D.LastAttemptTime}  datetime        NULL,
            {D.FailureReason}    MEDIUMTEXT      NULL,


            PRIMARY KEY ( {D.TessageId}, {D.EndpointId}),
                /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/

            FOREIGN KEY ({D.TessageId}) REFERENCES {tables.OutboxTessages} ({M.TessageId})
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;


       """;
}
