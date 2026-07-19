using Compze.Tessaging.Endpoints;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlPeerRegistrySqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager, EndpointTableSet tables) : ITessagingSqlLayer.IPeerRegistrySqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly EndpointTableSet _tables = tables;

   public async Task SaveAdvertisementAsync(EndpointId peerId, IReadOnlySet<string> handledTessageTypes)
   {
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT IGNORE INTO {_tables.Peers} ({Peers.EndpointId}) VALUES (@{Peers.EndpointId});

                   DELETE FROM {_tables.PeerHandledTessageTypes} WHERE {Types.EndpointId} = @{Types.EndpointId};

                   """)
              .AddParameter(Peers.EndpointId, peerId.Value);

            handledTessageTypes.ForEach(
               (handledTessageType, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {_tables.PeerHandledTessageTypes}
                                                            ({Types.EndpointId},  {Types.HandledTessageType})
                                                    VALUES (@{Types.EndpointId}, @{Types.HandledTessageType}_{index});

                                                """).AddMediumTextParameter($"{Types.HandledTessageType}_{index}", handledTessageType));

            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
   }

   public async Task<IReadOnlyList<ITessagingSqlLayer.PersistedPeer>> GetPeersAsync()
   {
      var rows = await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var raw = new List<(Guid EndpointId, string? HandledTessageType)>();

            command.SetCommandText(
               $"""

                SELECT p.{Peers.EndpointId}, t.{Types.HandledTessageType}
                FROM {_tables.Peers} p
                LEFT JOIN {_tables.PeerHandledTessageTypes} t ON p.{Peers.EndpointId} = t.{Types.EndpointId}

                """);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               raw.Add((reader.GetGuid(0), await reader.IsDBNullAsync(1).caf() ? null : reader.GetString(1)));
            }

            return raw;
         }).caf();

      return [..rows.GroupBy(row => row.EndpointId)
                    .Select(peer => new ITessagingSqlLayer.PersistedPeer(
                               new EndpointId(peer.Key),
                               peer.Where(row => row.HandledTessageType != null).Select(row => row.HandledTessageType!).ToHashSet()))];
   }

   public async Task DeletePeerAsync(EndpointId peerId)
   {
      await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        DELETE FROM {_tables.PeerHandledTessageTypes} WHERE {Types.EndpointId} = @{Types.EndpointId};
                        DELETE FROM {_tables.Peers} WHERE {Peers.EndpointId} = @{Peers.EndpointId};

                        """)
                   .AddParameter(Peers.EndpointId, peerId.Value)
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
