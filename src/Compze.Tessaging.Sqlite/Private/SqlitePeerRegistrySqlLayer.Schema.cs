using Compze.Tessaging.Internal.SqlLayer;
using PeersSchema = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite.Private;

partial class SqlitePeerRegistrySqlLayer
{
   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.Peers}
       (
           {PeersSchema.EndpointId} TEXT NOT NULL,

           PRIMARY KEY ( {PeersSchema.EndpointId} )
       );

       CREATE TABLE IF NOT EXISTS {tables.PeerHandledTessageTypes}
       (
           {Types.EndpointId}         TEXT NOT NULL,
           {Types.HandledTessageType} TEXT NOT NULL,

           FOREIGN KEY ( {Types.EndpointId} ) REFERENCES {tables.Peers} ({PeersSchema.EndpointId})
       );

       """;
}
