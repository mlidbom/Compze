using Compze.Tessaging._internal.SqlLayer;
using Tessage = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using D = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

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
           {D.TessageId}        TEXT    NOT NULL,
           {D.EndpointId}       TEXT    NOT NULL,
           {D.IsReceived}       INTEGER NOT NULL,
           {D.IsStranded}       INTEGER NOT NULL DEFAULT 0,
           {D.RetryCount}       INTEGER NOT NULL DEFAULT 0,
           {D.LastAttemptTime}  TEXT    NULL,
           {D.FailureReason}    TEXT    NULL,

           PRIMARY KEY( {D.TessageId}, {D.EndpointId}),
           FOREIGN KEY ( {D.TessageId} ) REFERENCES {tables.OutboxTessages} ({Tessage.TessageId})
       );

       """;
}
