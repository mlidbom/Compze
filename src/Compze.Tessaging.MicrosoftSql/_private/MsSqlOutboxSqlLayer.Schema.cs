using Compze.Tessaging._internal.SqlLayer;
using Outbox = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatching = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using Counter = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxDeliveryStreamCountersSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql._private;

partial class MsSqlOutboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{tables.OutboxTessages}')
       BEGIN
           CREATE TABLE {tables.OutboxTessages}
           (
               {Outbox.GeneratedId}       bigint IDENTITY(1,1) NOT NULL,
               {Outbox.TypeId}            int                  NOT NULL,
               {Outbox.TessageId}         uniqueidentifier     NOT NULL,
               {Outbox.SerializedTessage} nvarchar(MAX)        NOT NULL,

               CONSTRAINT PK_{tables.OutboxTessages} PRIMARY KEY CLUSTERED ( [{Outbox.GeneratedId}] ASC ),

               CONSTRAINT IX_{tables.OutboxTessages}_Unique_{Outbox.TessageId} UNIQUE ( {Outbox.TessageId} )
           )

           CREATE TABLE {tables.OutboxTessageDispatching}
           (
               {Dispatching.TessageId}                    uniqueidentifier NOT NULL,
               {Dispatching.EndpointId}                   uniqueidentifier NOT NULL,
               {Dispatching.DeliveryStreamSequenceNumber} bigint           NOT NULL,
               {Dispatching.IsReceived}                   bit              NOT NULL,
               {Dispatching.IsStranded}                   bit              NOT NULL DEFAULT 0,
               {Dispatching.RetryCount}                   int              NOT NULL DEFAULT 0,
               {Dispatching.LastAttemptTime}              datetime2        NULL,
               {Dispatching.FailureReason}                nvarchar(MAX)    NULL,

               CONSTRAINT PK_{tables.OutboxTessageDispatching} PRIMARY KEY CLUSTERED( {Dispatching.TessageId}, {Dispatching.EndpointId})
                   WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

               CONSTRAINT FK_{tables.OutboxTessageDispatching}_{Dispatching.TessageId} FOREIGN KEY ( {Dispatching.TessageId} )  REFERENCES {tables.OutboxTessages} ({Outbox.TessageId}),

               -- One position in a pair's delivery stream is one tessage: the loud backstop for the counter-assigned sequence.
               CONSTRAINT IX_{tables.Prefix}_OutboxStreamPosition UNIQUE ( {Dispatching.EndpointId}, {Dispatching.DeliveryStreamSequenceNumber} )
           )

           -- One row per receiver peer: the last sequence number assigned in that pair's delivery stream. The row's lock,
           -- taken by the save transaction's increment, serializes the pair's commits - sequence order is commit order.
           CREATE TABLE {tables.OutboxDeliveryStreamCounters}
           (
               {Counter.EndpointId}                 uniqueidentifier NOT NULL,
               {Counter.LastAssignedSequenceNumber} bigint           NOT NULL,

               CONSTRAINT PK_{tables.OutboxDeliveryStreamCounters} PRIMARY KEY CLUSTERED ( {Counter.EndpointId} )
           )
       END

       """;
}
