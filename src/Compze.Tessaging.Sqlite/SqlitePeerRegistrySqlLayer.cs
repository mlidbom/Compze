using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqlitePeerRegistrySqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IPeerRegistrySqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

   public async Task SaveAdvertisementAsync(EndpointId peerId, IReadOnlySet<string> handledTessageTypes)
   {
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT INTO {Peers.TableName} ({Peers.EndpointId}) VALUES (@{Peers.EndpointId})
                       ON CONFLICT ({Peers.EndpointId}) DO NOTHING;

                   DELETE FROM {Types.TableName} WHERE {Types.EndpointId} = @{Types.EndpointId};

                   """)
              .AddMediumTextParameter(Peers.EndpointId, peerId.ToString());

            handledTessageTypes.ForEach(
               (handledTessageType, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {Types.TableName}
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
                FROM {Peers.TableName} p
                LEFT JOIN {Types.TableName} t ON p.{Peers.EndpointId} = t.{Types.EndpointId}

                """);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               raw.Add((reader.GetGuidFromString(0), await reader.IsDBNullAsync(1).caf() ? null : reader.GetString(1)));
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

                        DELETE FROM {Types.TableName} WHERE {Types.EndpointId} = @{Types.EndpointId};
                        DELETE FROM {Peers.TableName} WHERE {Peers.EndpointId} = @{Peers.EndpointId};

                        """)
                   .AddMediumTextParameter(Peers.EndpointId, peerId.ToString())
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
