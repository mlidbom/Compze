using Tessage = Compze.ServiceBus.Transport.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqliteInboxSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Tessage.TableName}
       (
           {Tessage.GeneratedId}         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Tessage.TypeId}              INTEGER                           NOT NULL,
           {Tessage.TessageId}           TEXT                              NOT NULL UNIQUE,
           {Tessage.Status}              INTEGER                           NOT NULL,
           {Tessage.Body}                TEXT                              NOT NULL,
           {Tessage.ExceptionCount}      INTEGER                           NOT NULL DEFAULT 0,
           {Tessage.ExceptionType}       TEXT                              NULL,
           {Tessage.ExceptionStackTrace} TEXT                              NULL,
           {Tessage.ExceptionTessage}    TEXT                              NULL
       );

       """;
}
