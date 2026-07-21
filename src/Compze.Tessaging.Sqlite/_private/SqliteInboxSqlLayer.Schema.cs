using Compze.Tessaging._internal.SqlLayer;
using Tessage = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;
using Admissions = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxDeliveryStreamAdmissionsSchemaStrings;

namespace Compze.Tessaging.Sqlite._private;

partial class SqliteInboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.InboxTessages}
       (
           {Tessage.GeneratedId}                  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
           {Tessage.TypeId}                       INTEGER                           NOT NULL,
           {Tessage.TessageId}                    TEXT                              NOT NULL UNIQUE,
           {Tessage.SenderEndpointId}             TEXT                              NOT NULL,
           {Tessage.DeliveryStreamSequenceNumber} INTEGER                           NOT NULL,
           {Tessage.Status}                       INTEGER                           NOT NULL,
           {Tessage.Body}                         TEXT                              NOT NULL,
           {Tessage.ExceptionCount}               INTEGER                           NOT NULL DEFAULT 0,
           {Tessage.ExceptionType}                TEXT                              NULL,
           {Tessage.ExceptionStackTrace}          TEXT                              NULL,
           {Tessage.ExceptionTessage}             TEXT                              NULL,

           --One position in a pair's delivery stream is one admitted tessage: the loud backstop for the admission gate.
           CONSTRAINT IX_{tables.Prefix}_InboxStreamPosition UNIQUE ( {Tessage.SenderEndpointId}, {Tessage.DeliveryStreamSequenceNumber} )
       );

       --One row per sender peer: the pair's admission high-water mark. The inbox door admits only the next sequence
       --number, first contact starting at 1 - what makes exactly-once in-order admission hold by construction.
       CREATE TABLE IF NOT EXISTS {tables.InboxDeliveryStreamAdmissions}
       (
           {Admissions.SenderEndpointId}           TEXT    NOT NULL,
           {Admissions.LastAdmittedSequenceNumber} INTEGER NOT NULL,

           PRIMARY KEY ( {Admissions.SenderEndpointId} )
       );

       """;
}
