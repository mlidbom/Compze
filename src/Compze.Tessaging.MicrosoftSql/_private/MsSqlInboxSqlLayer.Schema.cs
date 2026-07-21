using Compze.Tessaging._internal.SqlLayer;
using Tessage = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;
using Admissions = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxDeliveryStreamAdmissionsSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql._private;

partial class MsSqlInboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       IF NOT EXISTS(select name from sys.tables where name = '{tables.InboxTessages}')
       BEGIN
           CREATE TABLE {tables.InboxTessages}
           (
               {Tessage.GeneratedId}                  bigint IDENTITY(1,1) NOT NULL,
               {Tessage.TypeId}                       int                  NOT NULL,
               {Tessage.TessageId}                    uniqueidentifier     NOT NULL,
               {Tessage.SenderEndpointId}             uniqueidentifier     NOT NULL,
               {Tessage.DeliveryStreamSequenceNumber} bigint               NOT NULL,
               {Tessage.Status}                       smallint             NOT NULL,
               {Tessage.Body}                         nvarchar(MAX)        NOT NULL,
               {Tessage.ExceptionCount}               int                  NOT NULL  DEFAULT 0,
               {Tessage.ExceptionType}                nvarchar(500)        NULL,
               {Tessage.ExceptionStackTrace}          nvarchar(MAX)        NULL,
               {Tessage.ExceptionTessage}             nvarchar(MAX)        NULL,


               CONSTRAINT PK_{tables.InboxTessages} PRIMARY KEY CLUSTERED ( [{Tessage.GeneratedId}] ASC ),

               CONSTRAINT IX_{tables.InboxTessages}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} ),

               -- One position in a pair's delivery stream is one admitted tessage: the loud backstop for the admission gate.
               CONSTRAINT IX_{tables.Prefix}_InboxStreamPosition UNIQUE ( {Tessage.SenderEndpointId}, {Tessage.DeliveryStreamSequenceNumber} )
           )

           -- One row per sender peer: the pair's admission high-water mark. The inbox door admits only the next sequence
           -- number, first contact starting at 1 - what makes exactly-once in-order admission hold by construction.
           CREATE TABLE {tables.InboxDeliveryStreamAdmissions}
           (
               {Admissions.SenderEndpointId}           uniqueidentifier NOT NULL,
               {Admissions.LastAdmittedSequenceNumber} bigint           NOT NULL,

               CONSTRAINT PK_{tables.InboxDeliveryStreamAdmissions} PRIMARY KEY CLUSTERED ( {Admissions.SenderEndpointId} )
           )
       END

       """;
}
