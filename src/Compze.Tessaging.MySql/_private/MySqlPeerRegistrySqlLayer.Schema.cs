using Compze.Tessaging._internal.SqlLayer;
using PeersSchema = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql._private;

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
