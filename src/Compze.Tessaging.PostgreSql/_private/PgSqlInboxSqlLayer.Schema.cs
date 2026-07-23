using Compze.Tessaging._internal.SqlLayer;
using Tessage = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;
using Admissions = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxDeliveryStreamAdmissionsSchemaStrings;

namespace Compze.Tessaging.PostgreSql._private;

partial class PgSqlInboxSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""
           CREATE TABLE IF NOT EXISTS {tables.InboxTessages}
           (
               {Tessage.GeneratedId}                  bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
               {Tessage.TypeId}                       int                                 NOT NULL,
               {Tessage.TessageId}                    {PgSqlGuidType}                     NOT NULL,
               {Tessage.SenderEndpointId}             {PgSqlGuidType}                     NOT NULL,
               {Tessage.DeliveryStreamSequenceNumber} bigint                              NOT NULL,
               {Tessage.Status}                       smallint                            NOT NULL,
               {Tessage.Body}                         text                                NOT NULL,
               {Tessage.ExceptionCount}               int                                 NOT NULL  DEFAULT 0,
               {Tessage.ExceptionType}                varchar(500)                        NULL,
               {Tessage.ExceptionStackTrace}          text                                NULL,
               {Tessage.ExceptionTessage}             text                                NULL,


               PRIMARY KEY ( {Tessage.GeneratedId} ),

               CONSTRAINT IX_{tables.InboxTessages}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} ),

               -- One position in a pair's delivery stream is one admitted tessage: the loud backstop for the admission gate.
               CONSTRAINT IX_{tables.Prefix}_InboxStreamPosition UNIQUE ( {Tessage.SenderEndpointId}, {Tessage.DeliveryStreamSequenceNumber} )
           );

           -- One row per sender peer: the pair's admission high-water mark. The inbox admits only the next sequence
           -- number, first contact starting at 1 - what makes exactly-once in-order admission hold by construction.
           CREATE TABLE IF NOT EXISTS {tables.InboxDeliveryStreamAdmissions}
           (
               {Admissions.SenderEndpointId}           {PgSqlGuidType} NOT NULL,
               {Admissions.LastAdmittedSequenceNumber} bigint          NOT NULL,

               PRIMARY KEY ( {Admissions.SenderEndpointId} )
           );

       """;
}
