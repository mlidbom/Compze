using Tessage = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatch = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlOutboxSqlLayer
{
   const string PgSqlGuidType = "UUID";

   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Tessage.TableName}
       (
         {Tessage.GeneratedId}       bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
         {Tessage.TypeId}            int                                 NOT NULL,
         {Tessage.TessageId}         {PgSqlGuidType}                     NOT NULL,
         {Tessage.SerializedTessage} TEXT                                NOT NULL,

         PRIMARY KEY ({Tessage.GeneratedId}),

         CONSTRAINT IX_{Tessage.TableName}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
       );


       CREATE TABLE  IF NOT EXISTS {Dispatch.TableName}
       (
        {Dispatch.TessageId}        {PgSqlGuidType} NOT NULL,
        {Dispatch.EndpointId}       {PgSqlGuidType} NOT NULL,
        {Dispatch.IsReceived}       boolean         NOT NULL,
        {Dispatch.IsStranded}       boolean         NOT NULL DEFAULT false,
        {Dispatch.RetryCount}       integer         NOT NULL DEFAULT 0,
        {Dispatch.LastAttemptTime}  timestamptz     NULL,
        {Dispatch.FailureReason}    TEXT            NULL,


        PRIMARY KEY ( {Dispatch.TessageId}, {Dispatch.EndpointId}),
         FOREIGN KEY ({Dispatch.TessageId}) REFERENCES {Tessage.TableName} ({Tessage.TessageId})
       );

       """;
}
