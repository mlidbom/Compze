using Compze.Tessaging.Internal.SqlLayer;
using Tessage = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite.Private;

partial class SqliteInboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.InboxTessages}
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
