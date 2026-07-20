using Compze.Tessaging.Internal.SqlLayer;
using Tessage = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatch = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlOutboxSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.OutboxTessages}
       (
         {Tessage.GeneratedId}       bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
         {Tessage.TypeId}            int                                 NOT NULL,
         {Tessage.TessageId}         {PgSqlGuidType}                     NOT NULL,
         {Tessage.SerializedTessage} TEXT                                NOT NULL,

         PRIMARY KEY ({Tessage.GeneratedId}),

         CONSTRAINT IX_{tables.OutboxTessages}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
       );


       CREATE TABLE  IF NOT EXISTS {tables.OutboxTessageDispatching}
       (
        {Dispatch.TessageId}        {PgSqlGuidType} NOT NULL,
        {Dispatch.EndpointId}       {PgSqlGuidType} NOT NULL,
        {Dispatch.IsReceived}       boolean         NOT NULL,
        {Dispatch.IsStranded}       boolean         NOT NULL DEFAULT false,
        {Dispatch.RetryCount}       integer         NOT NULL DEFAULT 0,
        {Dispatch.LastAttemptTime}  timestamptz     NULL,
        {Dispatch.FailureReason}    TEXT            NULL,


        PRIMARY KEY ( {Dispatch.TessageId}, {Dispatch.EndpointId}),
         FOREIGN KEY ({Dispatch.TessageId}) REFERENCES {tables.OutboxTessages} ({Tessage.TessageId})
       );

       """;
}
