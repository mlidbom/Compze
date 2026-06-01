using M = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using D = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlOutboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public const string SchemaCreationSql =
      $"""

        CREATE TABLE IF NOT EXISTS {M.TableName}
        (
            {M.GeneratedId}       bigint          NOT NULL  AUTO_INCREMENT,
            {M.TypeId}            int             NOT NULL,
            {M.TessageId}         {MySqlGuidType} NOT NULL,
            {M.SerializedTessage} MEDIUMTEXT      NOT NULL,

            PRIMARY KEY ( {M.GeneratedId}),

            UNIQUE INDEX IX_{M.TableName}_Unique_{M.TessageId} ( {M.TessageId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

        CREATE TABLE  IF NOT EXISTS {D.TableName}
        (
            {D.TessageId}        {MySqlGuidType} NOT NULL,
            {D.EndpointId}       {MySqlGuidType} NOT NULL,
            {D.IsReceived}       bit             NOT NULL,
            {D.RetryCount}       int             NOT NULL DEFAULT 0,
            {D.LastAttemptTime}  datetime        NULL,
            {D.FailureReason}    MEDIUMTEXT      NULL,


            PRIMARY KEY ( {D.TessageId}, {D.EndpointId}),
                /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/

            FOREIGN KEY ({D.TessageId}) REFERENCES {M.TableName} ({M.TessageId})
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;


       """;
}
