using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqlitePeerRegistrySqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IPeerRegistrySqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

   public void SaveAdvertisement(EndpointId peerId, IReadOnlySet<string> handledTessageTypes)
   {
      _connectionFactory.UseCommand(
         command =>
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

            command.ExecuteNonQuery();
         });
   }

   public IReadOnlyList<IServiceBusSqlLayer.PersistedPeer> GetPeers()
   {
      var rows = _connectionFactory.UseCommand(
         command =>
         {
            var raw = new List<(Guid EndpointId, string? HandledTessageType)>();

            command.SetCommandText(
               $"""

                SELECT p.{Peers.EndpointId}, t.{Types.HandledTessageType}
                FROM {Peers.TableName} p
                LEFT JOIN {Types.TableName} t ON p.{Peers.EndpointId} = t.{Types.EndpointId}

                """);

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               raw.Add((reader.GetGuidFromString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return raw;
         });

      return [..rows.GroupBy(row => row.EndpointId)
                    .Select(peer => new IServiceBusSqlLayer.PersistedPeer(
                               new EndpointId(peer.Key),
                               peer.Where(row => row.HandledTessageType != null).Select(row => row.HandledTessageType!).ToHashSet()))];
   }

   public void DeletePeer(EndpointId peerId)
   {
      _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        DELETE FROM {Types.TableName} WHERE {Types.EndpointId} = @{Types.EndpointId};
                        DELETE FROM {Peers.TableName} WHERE {Peers.EndpointId} = @{Peers.EndpointId};

                        """)
                   .AddMediumTextParameter(Peers.EndpointId, peerId.ToString())
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
