using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Microsoft.Data.SqlClient;
using Catalog = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlEndpointCatalogSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddNVarcharMaxParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddParameter("filter", endpointId.Value)).caf()).SingleOrDefault();

   public async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> GetEntriesAsync() =>
      await EntriesAsync(filterClause: "", addFilterParameter: _ => {}).caf();

   public async Task<bool> TryInsertEntryHoldingTheLeaseAsync(string endpointName, EndpointId endpointId, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //UPDLOCK+HOLDLOCK makes the existence check and the insert one serialized act, so a racing
                       //registration cannot slip between them - the loser sees the row and inserts nothing.
                       $"""

                        INSERT INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc},  {Catalog.LeaseHolderId},  {Catalog.LeaseHolderDescription},  {Catalog.LeaseHeartbeatUtc})
                             SELECT @{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc}, @{Catalog.LeaseHolderId}, @{Catalog.LeaseHolderDescription}, @{Catalog.LeaseHeartbeatUtc}
                        WHERE NOT EXISTS (SELECT 1 FROM {Catalog.TableName} WITH (UPDLOCK, HOLDLOCK) WHERE {Catalog.EndpointName} = @{Catalog.EndpointName})

                        """)
                   .AddNVarcharMaxParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.EndpointId, endpointId.Value)
                   .AddDateTime2Parameter(Catalog.CreatedUtc, utcNow)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .AddNVarcharMaxParameter(Catalog.LeaseHolderDescription, leaseHolderDescription)
                   .AddDateTime2Parameter(Catalog.LeaseHeartbeatUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task<bool> TryTakeTheLeaseAsync(string endpointName, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow, DateTime staleBefore) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LeaseHolderId} = @{Catalog.LeaseHolderId},
                                {Catalog.LeaseHolderDescription} = @{Catalog.LeaseHolderDescription},
                                {Catalog.LeaseHeartbeatUtc} = @{Catalog.LeaseHeartbeatUtc}
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}
                          AND ({Catalog.LeaseHolderId} IS NULL OR {Catalog.LeaseHeartbeatUtc} < @StaleBefore)

                        """)
                   .AddNVarcharMaxParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .AddNVarcharMaxParameter(Catalog.LeaseHolderDescription, leaseHolderDescription)
                   .AddDateTime2Parameter(Catalog.LeaseHeartbeatUtc, utcNow)
                   .AddDateTime2Parameter("StaleBefore", staleBefore)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task<bool> TryHeartbeatAsync(string endpointName, Guid leaseHolderId, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LeaseHeartbeatUtc} = @{Catalog.LeaseHeartbeatUtc}
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}
                          AND {Catalog.LeaseHolderId} = @{Catalog.LeaseHolderId}

                        """)
                   .AddNVarcharMaxParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .AddDateTime2Parameter(Catalog.LeaseHeartbeatUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task ReleaseTheLeaseAsync(string endpointName, Guid leaseHolderId) =>
      await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LeaseHolderId} = NULL,
                                {Catalog.LeaseHolderDescription} = NULL,
                                {Catalog.LeaseHeartbeatUtc} = NULL
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}
                          AND {Catalog.LeaseHolderId} = @{Catalog.LeaseHolderId}

                        """)
                   .AddNVarcharMaxParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .ExecuteNonQueryAsync().caf()).caf();

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<SqlCommand> addFilterParameter) =>
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var entries = new List<ITessagingSqlLayer.EndpointCatalogEntry>();

            command.SetCommandText(
               $"""

                SELECT {Catalog.EndpointName}, {Catalog.EndpointId}, {Catalog.CreatedUtc}, {Catalog.LeaseHolderDescription}, {Catalog.LeaseHeartbeatUtc}
                FROM {Catalog.TableName} {filterClause}

                """);
            addFilterParameter(command);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               entries.Add(new ITessagingSqlLayer.EndpointCatalogEntry(
                              endpointName: reader.GetString(0),
                              endpointId: new EndpointId(reader.GetGuid(1)),
                              createdUtc: DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                              leaseHolderDescription: await reader.IsDBNullAsync(3).caf() ? null : reader.GetString(3),
                              leaseHeartbeatUtc: await reader.IsDBNullAsync(4).caf() ? null : DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)));
            }

            return entries;
         }).caf();

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
