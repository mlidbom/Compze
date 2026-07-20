using Compze.Tessaging.Internal.SqlLayer;
using PeersSchema = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlPeerRegistrySqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

        CREATE TABLE IF NOT EXISTS {tables.Peers}
        (
            {PeersSchema.EndpointId} {MySqlGuidType} NOT NULL,

            PRIMARY KEY ( {PeersSchema.EndpointId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

        CREATE TABLE IF NOT EXISTS {tables.PeerHandledTessageTypes}
        (
            {Types.EndpointId}         {MySqlGuidType} NOT NULL,
            {Types.HandledTessageType} MEDIUMTEXT      NOT NULL,

            FOREIGN KEY ({Types.EndpointId}) REFERENCES {tables.Peers} ({PeersSchema.EndpointId})
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

       """;
}
