using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlPeerRegistrySqlLayer
{
   const string PgSqlGuidType = "UUID";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.Peers}
       (
         {Peers.EndpointId} {PgSqlGuidType} NOT NULL,

         PRIMARY KEY ( {Peers.EndpointId} )
       );

       CREATE TABLE IF NOT EXISTS {tables.PeerHandledTessageTypes}
       (
        {Types.EndpointId}         {PgSqlGuidType} NOT NULL,
        {Types.HandledTessageType} TEXT            NOT NULL,

        FOREIGN KEY ({Types.EndpointId}) REFERENCES {tables.Peers} ({Peers.EndpointId})
       );

       """;
}
