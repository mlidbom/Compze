using Tessage = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using D = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.Tessaging;

partial class SqliteOutboxSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Tessage.TableName}
       (
           {Tessage.GeneratedId}       INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Tessage.TypeIdGuidValue}   TEXT                              NOT NULL,
           {Tessage.TessageId}         TEXT                              NOT NULL UNIQUE,
           {Tessage.SerializedTessage} TEXT                              NOT NULL
       );

       CREATE TABLE IF NOT EXISTS {D.TableName}
       (
           {D.TessageId}        TEXT    NOT NULL,
           {D.EndpointId}       TEXT    NOT NULL,
           {D.IsReceived}       INTEGER NOT NULL,
           {D.RetryCount}       INTEGER NOT NULL DEFAULT 0,
           {D.LastAttemptTime}  TEXT    NULL,
           {D.FailureReason}    TEXT    NULL,

           PRIMARY KEY( {D.TessageId}, {D.EndpointId}),
           FOREIGN KEY ( {D.TessageId} ) REFERENCES {Tessage.TableName} ({Tessage.TessageId})
       );

       """;
}
