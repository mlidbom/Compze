using Compze.Tessaging.Transport.SqlLayer;
using Outbox = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatching = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlOutboxSqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{tables.OutboxTessages}')
       BEGIN
           CREATE TABLE {tables.OutboxTessages}
           (
               {Outbox.GeneratedId}       bigint IDENTITY(1,1) NOT NULL,
               {Outbox.TypeId}            int                  NOT NULL,
               {Outbox.TessageId}         uniqueidentifier     NOT NULL,
               {Outbox.SerializedTessage} nvarchar(MAX)        NOT NULL,

               CONSTRAINT PK_{tables.OutboxTessages} PRIMARY KEY CLUSTERED ( [{Outbox.GeneratedId}] ASC ),

               CONSTRAINT IX_{tables.OutboxTessages}_Unique_{Outbox.TessageId} UNIQUE ( {Outbox.TessageId} )
           )

           CREATE TABLE {tables.OutboxTessageDispatching}
           (
               {Dispatching.TessageId}        uniqueidentifier NOT NULL,
               {Dispatching.EndpointId}       uniqueidentifier NOT NULL,
               {Dispatching.IsReceived}       bit              NOT NULL,
               {Dispatching.IsStranded}       bit              NOT NULL DEFAULT 0,
               {Dispatching.RetryCount}       int              NOT NULL DEFAULT 0,
               {Dispatching.LastAttemptTime}  datetime2        NULL,
               {Dispatching.FailureReason}    nvarchar(MAX)    NULL,

               CONSTRAINT PK_{tables.OutboxTessageDispatching} PRIMARY KEY CLUSTERED( {Dispatching.TessageId}, {Dispatching.EndpointId})
                   WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

               CONSTRAINT FK_{tables.OutboxTessageDispatching}_{Dispatching.TessageId} FOREIGN KEY ( {Dispatching.TessageId} )  REFERENCES {tables.OutboxTessages} ({Outbox.TessageId})
           )
       END

       """;
}
