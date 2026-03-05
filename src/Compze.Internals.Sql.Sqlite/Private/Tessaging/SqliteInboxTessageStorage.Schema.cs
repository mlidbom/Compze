using Tessage = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.Tessaging;

partial class SqliteInboxSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Tessage.TableName}
       (
           {Tessage.GeneratedId}         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Tessage.TypeId}              TEXT                              NOT NULL,
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
