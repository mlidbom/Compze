using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqlitePeerRegistrySqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.Peers}
       (
           {Peers.EndpointId} TEXT NOT NULL,

           PRIMARY KEY ( {Peers.EndpointId} )
       );

       CREATE TABLE IF NOT EXISTS {tables.PeerHandledTessageTypes}
       (
           {Types.EndpointId}         TEXT NOT NULL,
           {Types.HandledTessageType} TEXT NOT NULL,

           FOREIGN KEY ( {Types.EndpointId} ) REFERENCES {tables.Peers} ({Peers.EndpointId})
       );

       """;
}
