using Compze.Tessaging._internal.SqlLayer;
using PeersSchema = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql._private;

partial class MsSqlPeerRegistrySqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{tables.Peers}')
       BEGIN
           CREATE TABLE {tables.Peers}
           (
               {PeersSchema.EndpointId} uniqueidentifier NOT NULL,

               CONSTRAINT PK_{tables.Peers} PRIMARY KEY CLUSTERED ( {PeersSchema.EndpointId} )
           )

           CREATE TABLE {tables.PeerHandledTessageTypes}
           (
               {Types.EndpointId}         uniqueidentifier NOT NULL,
               {Types.HandledTessageType} nvarchar(MAX)    NOT NULL,

               CONSTRAINT FK_{tables.PeerHandledTessageTypes}_{Types.EndpointId} FOREIGN KEY ( {Types.EndpointId} ) REFERENCES {tables.Peers} ({PeersSchema.EndpointId})
           )

           CREATE INDEX IX_{tables.PeerHandledTessageTypes}_{Types.EndpointId} ON {tables.PeerHandledTessageTypes} ( {Types.EndpointId} )
       END

       """;
}
