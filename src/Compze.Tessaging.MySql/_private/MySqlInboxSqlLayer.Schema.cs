using Compze.Tessaging._internal.SqlLayer;
using T = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;
using A = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxDeliveryStreamAdmissionsSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlInboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

           CREATE TABLE IF NOT EXISTS {tables.InboxTessages}
           (
               {T.GeneratedId}                  bigint          NOT NULL  AUTO_INCREMENT,
               {T.TypeId}                       int             NOT NULL,
               {T.TessageId}                    {MySqlGuidType} NOT NULL,
               {T.SenderEndpointId}             {MySqlGuidType} NOT NULL,
               {T.DeliveryStreamSequenceNumber} bigint          NOT NULL,
               {T.Status}                       smallint        NOT NULL,
               {T.Body}                         mediumtext      NOT NULL,
               {T.ExceptionCount}               int             NOT NULL  DEFAULT 0,
               {T.ExceptionType}                varchar(500)    NULL,
               {T.ExceptionStackTrace}          mediumtext      NULL,
               {T.ExceptionTessage}             mediumtext      NULL,


               PRIMARY KEY ( {T.GeneratedId} ),

               UNIQUE INDEX IX_{tables.InboxTessages}_Unique_{T.TessageId} ( {T.TessageId} ),

               -- One position in a pair's delivery stream is one admitted tessage: the loud backstop for the admission gate.
               UNIQUE INDEX IX_{tables.Prefix}_InboxStreamPosition ( {T.SenderEndpointId}, {T.DeliveryStreamSequenceNumber} )
           )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;

           -- One row per sender peer: the pair's admission high-water mark. The inbox door admits only the next sequence
           -- number, first contact starting at 1 - what makes exactly-once in-order admission hold by construction.
           CREATE TABLE IF NOT EXISTS {tables.InboxDeliveryStreamAdmissions}
           (
               {A.SenderEndpointId}           {MySqlGuidType} NOT NULL,
               {A.LastAdmittedSequenceNumber} bigint          NOT NULL,

               PRIMARY KEY ( {A.SenderEndpointId} )
           )
       ENGINE = InnoDB
       DEFAULT CHARACTER SET = utf8mb4;


       """;
}
