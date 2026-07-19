using Compze.Tessaging.Transport.SqlLayer;
using Tessage = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlInboxSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""
           CREATE TABLE IF NOT EXISTS {tables.InboxTessages}
           (
               {Tessage.GeneratedId}           bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
               {Tessage.TypeId}                int                                 NOT NULL,
               {Tessage.TessageId}             {PgSqlGuidType}                     NOT NULL,
               {Tessage.Status}                smallint                            NOT NULL,
               {Tessage.Body}                  text                                NOT NULL,
               {Tessage.ExceptionCount}        int                                 NOT NULL  DEFAULT 0,
               {Tessage.ExceptionType}         varchar(500)                        NULL,
               {Tessage.ExceptionStackTrace}   text                                NULL,
               {Tessage.ExceptionTessage}      text                                NULL,


               PRIMARY KEY ( {Tessage.GeneratedId} ),

               CONSTRAINT IX_{tables.InboxTessages}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
           );

       """;
}
