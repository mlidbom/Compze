using Tessage = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatch = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Internals.Sql.PostgreSql.Private.Tessaging;

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
        {Dispatch.RetryCount}       integer         NOT NULL DEFAULT 0,
        {Dispatch.LastAttemptTime}  timestamptz     NULL,
        {Dispatch.FailureReason}    TEXT            NULL,


        PRIMARY KEY ( {Dispatch.TessageId}, {Dispatch.EndpointId}),
         FOREIGN KEY ({Dispatch.TessageId}) REFERENCES {Tessage.TableName} ({Tessage.TessageId})
       );

       """;
}
