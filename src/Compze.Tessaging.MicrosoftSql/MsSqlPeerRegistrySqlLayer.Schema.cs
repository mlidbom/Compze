using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlPeerRegistrySqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{tables.Peers}')
       BEGIN
           CREATE TABLE {tables.Peers}
           (
               {Peers.EndpointId} uniqueidentifier NOT NULL,

               CONSTRAINT PK_{tables.Peers} PRIMARY KEY CLUSTERED ( {Peers.EndpointId} )
           )

           CREATE TABLE {tables.PeerHandledTessageTypes}
           (
               {Types.EndpointId}         uniqueidentifier NOT NULL,
               {Types.HandledTessageType} nvarchar(MAX)    NOT NULL,

               CONSTRAINT FK_{tables.PeerHandledTessageTypes}_{Types.EndpointId} FOREIGN KEY ( {Types.EndpointId} ) REFERENCES {tables.Peers} ({Peers.EndpointId})
           )

           CREATE INDEX IX_{tables.PeerHandledTessageTypes}_{Types.EndpointId} ON {tables.PeerHandledTessageTypes} ( {Types.EndpointId} )
       END

       """;
}
