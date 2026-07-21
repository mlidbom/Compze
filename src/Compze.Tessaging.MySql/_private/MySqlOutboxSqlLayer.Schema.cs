using Compze.Tessaging._internal.SqlLayer;
using M = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using D = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using C = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxDeliveryStreamCountersSchemaStrings;

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
            {D.TessageId}                    {MySqlGuidType} NOT NULL,
            {D.EndpointId}                   {MySqlGuidType} NOT NULL,
            {D.DeliveryStreamSequenceNumber} bigint          NOT NULL,
            {D.IsReceived}                   bit             NOT NULL,
            {D.IsStranded}                   bit             NOT NULL DEFAULT 0,
            {D.RetryCount}                   int             NOT NULL DEFAULT 0,
            {D.LastAttemptTime}              datetime        NULL,
            {D.FailureReason}                MEDIUMTEXT      NULL,


            PRIMARY KEY ( {D.TessageId}, {D.EndpointId}),
                /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/

            FOREIGN KEY ({D.TessageId}) REFERENCES {tables.OutboxTessages} ({M.TessageId}),

            -- One position in a pair's delivery stream is one tessage: the loud backstop for the counter-assigned sequence.
            UNIQUE INDEX IX_{tables.Prefix}_OutboxStreamPosition ( {D.EndpointId}, {D.DeliveryStreamSequenceNumber} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

        -- One row per receiver peer: the last sequence number assigned in that pair's delivery stream. The row's lock,
        -- taken by the save transaction's increment, serializes the pair's commits - sequence order is commit order.
        CREATE TABLE IF NOT EXISTS {tables.OutboxDeliveryStreamCounters}
        (
            {C.EndpointId}                 {MySqlGuidType} NOT NULL,
            {C.LastAssignedSequenceNumber} bigint          NOT NULL,

            PRIMARY KEY ( {C.EndpointId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;


       """;
}
