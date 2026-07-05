using Tessage = Compze.ServiceBus.Transport.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.ServiceBus.PostgreSql;

partial class PgSqlInboxSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public const string SchemaCreationSql =
      $"""
           CREATE TABLE IF NOT EXISTS {Tessage.TableName}
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

               CONSTRAINT IX_{Tessage.TableName}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
           );

       """;
}
