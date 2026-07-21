using Compze.Tessaging._internal.SqlLayer;
using Tessage = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatch = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using Counter = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxDeliveryStreamCountersSchemaStrings;

namespace Compze.Tessaging.PostgreSql._private;

partial class PgSqlOutboxSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.OutboxTessages}
       (
         {Tessage.GeneratedId}       bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
         {Tessage.TypeId}            int                                 NOT NULL,
         {Tessage.TessageId}         {PgSqlGuidType}                     NOT NULL,
         {Tessage.SerializedTessage} TEXT                                NOT NULL,

         PRIMARY KEY ({Tessage.GeneratedId}),

         CONSTRAINT IX_{tables.OutboxTessages}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
       );


       CREATE TABLE  IF NOT EXISTS {tables.OutboxTessageDispatching}
       (
        {Dispatch.TessageId}                    {PgSqlGuidType} NOT NULL,
        {Dispatch.EndpointId}                   {PgSqlGuidType} NOT NULL,
        {Dispatch.DeliveryStreamSequenceNumber} bigint          NOT NULL,
        {Dispatch.IsReceived}                   boolean         NOT NULL,
        {Dispatch.IsStranded}                   boolean         NOT NULL DEFAULT false,
        {Dispatch.RetryCount}                   integer         NOT NULL DEFAULT 0,
        {Dispatch.LastAttemptTime}              timestamptz     NULL,
        {Dispatch.FailureReason}                TEXT            NULL,


        PRIMARY KEY ( {Dispatch.TessageId}, {Dispatch.EndpointId}),
         FOREIGN KEY ({Dispatch.TessageId}) REFERENCES {tables.OutboxTessages} ({Tessage.TessageId}),

         -- One position in a pair's delivery stream is one tessage: the loud backstop for the counter-assigned sequence.
         CONSTRAINT IX_{tables.Prefix}_OutboxStreamPosition UNIQUE ( {Dispatch.EndpointId}, {Dispatch.DeliveryStreamSequenceNumber} )
       );

       -- One row per receiver peer: the last sequence number assigned in that pair's delivery stream. The row's lock,
       -- taken by the save transaction's increment, serializes the pair's commits - sequence order is commit order.
       CREATE TABLE IF NOT EXISTS {tables.OutboxDeliveryStreamCounters}
       (
        {Counter.EndpointId}                 {PgSqlGuidType} NOT NULL,
        {Counter.LastAssignedSequenceNumber} bigint          NOT NULL,

        PRIMARY KEY ( {Counter.EndpointId} )
       );

       """;
}
