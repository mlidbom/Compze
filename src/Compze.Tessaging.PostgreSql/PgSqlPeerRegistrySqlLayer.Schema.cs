using Compze.Tessaging.Internal.SqlLayer;
using PeersSchema = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlPeerRegistrySqlLayer
{
   const string PgSqlGuidType = "UUID";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

       CREATE TABLE IF NOT EXISTS {tables.Peers}
       (
         {PeersSchema.EndpointId} {PgSqlGuidType} NOT NULL,

         PRIMARY KEY ( {PeersSchema.EndpointId} )
       );

       CREATE TABLE IF NOT EXISTS {tables.PeerHandledTessageTypes}
       (
        {Types.EndpointId}         {PgSqlGuidType} NOT NULL,
        {Types.HandledTessageType} TEXT            NOT NULL,

        FOREIGN KEY ({Types.EndpointId}) REFERENCES {tables.Peers} ({PeersSchema.EndpointId})
       );

       """;
}
