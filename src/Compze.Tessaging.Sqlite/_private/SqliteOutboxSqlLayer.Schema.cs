using Compze.Tessaging._internal.SqlLayer;
using Tessage = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using D = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using C = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxDeliveryStreamCountersSchemaStrings;

namespace Compze.Tessaging.Sqlite._private;

partial class SqliteOutboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.OutboxTessages}
       (
           {Tessage.GeneratedId}       INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Tessage.TypeId}            INTEGER                           NOT NULL,
           {Tessage.TessageId}         TEXT                              NOT NULL UNIQUE,
           {Tessage.SerializedTessage} TEXT                              NOT NULL
       );

       CREATE TABLE IF NOT EXISTS {tables.OutboxTessageDispatching}
       (
           {D.TessageId}                    TEXT    NOT NULL,
           {D.EndpointId}                   TEXT    NOT NULL,
           {D.DeliveryStreamSequenceNumber} INTEGER NOT NULL,
           {D.IsReceived}                   INTEGER NOT NULL,
           {D.IsStranded}                   INTEGER NOT NULL DEFAULT 0,
           {D.RetryCount}                   INTEGER NOT NULL DEFAULT 0,
           {D.LastAttemptTime}              TEXT    NULL,
           {D.FailureReason}                TEXT    NULL,

           PRIMARY KEY( {D.TessageId}, {D.EndpointId}),
           FOREIGN KEY ( {D.TessageId} ) REFERENCES {tables.OutboxTessages} ({Tessage.TessageId}),

           --One position in a pair's delivery stream is one tessage: the loud backstop for the counter-assigned sequence.
           CONSTRAINT IX_{tables.Prefix}_OutboxStreamPosition UNIQUE ( {D.EndpointId}, {D.DeliveryStreamSequenceNumber} )
       );

       --One row per receiver peer: the last sequence number assigned in that pair's delivery stream. The pair's commits
       --serialize on SQLite's per-database write lock, so sequence order is commit order here too.
       CREATE TABLE IF NOT EXISTS {tables.OutboxDeliveryStreamCounters}
       (
           {C.EndpointId}                 TEXT    NOT NULL,
           {C.LastAssignedSequenceNumber} INTEGER NOT NULL,

           PRIMARY KEY ( {C.EndpointId} )
       );

       """;
}
