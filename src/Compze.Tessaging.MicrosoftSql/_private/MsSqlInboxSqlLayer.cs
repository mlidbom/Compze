using Compze.Sql.Common._internal;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging._internal.Transport;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Interning;
using TessageTable = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;
using Admissions = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.InboxDeliveryStreamAdmissionsSchemaStrings;
using Compze.Tessaging._private.SqlLayer;

namespace Compze.Tessaging.MicrosoftSql._private;

partial class MsSqlInboxSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) : ITessagingSqlLayer.IInboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;
   readonly EndpointTableSet _tables = tables;

   public async Task<ITessagingSqlLayer.SaveTessageResult> SaveTessageAsync(TessageId tessageId, TypeId typeId, string serializedTessage, DeliveryStreamPosition deliveryStreamPosition)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var internedTypeId = _typeIdInterner.GetOrInternId(typeId);
      //Admission and registration are one atomic act: the high-water mark advance and the tessage row commit together,
      //so no crash or race can admit a tessage without registering it, or register one out of stream order.
      return await TransactionScopeCe.ExecuteAsync(async () =>
      {
         if(!await TryAdvanceAdmissionHighWaterMarkAsync(deliveryStreamPosition).caf())
            //One position in the stream is one tessage, so at or below the mark means this very tessage was admitted
            //already - a redelivery to acknowledge. Above it, its declared predecessor has not been admitted: refuse.
            return deliveryStreamPosition.SequenceNumber <= await GetLastAdmittedSequenceNumberAsync(deliveryStreamPosition).caf()
                      ? ITessagingSqlLayer.SaveTessageResult.Duplicate
                      : ITessagingSqlLayer.SaveTessageResult.RefusedAwaitingItsPredecessor;

         await _connectionFactory.UseCommandAsync(
            async command => await command
                      .SetCommandText(
                          $"""

                           INSERT INTO {_tables.InboxTessages}
                                       ({TessageTable.TessageId},  {TessageTable.TypeId},  {TessageTable.SenderEndpointId},  {TessageTable.DeliveryStreamSequenceNumber},  {TessageTable.Body}, {TessageTable.Status})
                               VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.SenderEndpointId}, @{TessageTable.DeliveryStreamSequenceNumber}, @{TessageTable.Body}, {(int)InboxTessageStatus.UnHandled})

                           """)
                      .AddParameter(TessageTable.TessageId, tessageId.Value)
                      .AddParameter(TessageTable.TypeId, internedTypeId)
                      .AddParameter(TessageTable.SenderEndpointId, deliveryStreamPosition.SenderEndpointId.Value)
                      .AddParameter(TessageTable.DeliveryStreamSequenceNumber, deliveryStreamPosition.SequenceNumber)
                       //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
                      .AddNVarcharMaxParameter(TessageTable.Body, serializedTessage)
                      .ExecuteNonQueryAsync().caf()).caf();

         return ITessagingSqlLayer.SaveTessageResult.NewTessage;
      }).caf();
   }

   //The pair's admission gate: advance the high-water mark iff it equals the tessage's declared predecessor - the previous
   //stream member the sender's durable rows still name, so sender-side pruning holes are crossed exactly when real. The
   //winning UPDATE's row lock serializes racing same-pair admissions; first contact - no row yet - requires a declared
   //predecessor of 0.
   async Task<bool> TryAdvanceAdmissionHighWaterMarkAsync(DeliveryStreamPosition position)
   {
      var advanced = await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.InboxDeliveryStreamAdmissions}
                            SET {Admissions.LastAdmittedSequenceNumber} = @{Admissions.LastAdmittedSequenceNumber}
                        WHERE {Admissions.SenderEndpointId} = @{Admissions.SenderEndpointId}
                            AND {Admissions.LastAdmittedSequenceNumber} = @PredecessorSequenceNumber

                        """)
                   .AddParameter(Admissions.SenderEndpointId, position.SenderEndpointId.Value)
                   .AddParameter(Admissions.LastAdmittedSequenceNumber, position.SequenceNumber)
                   .AddParameter("PredecessorSequenceNumber", position.PredecessorSequenceNumber)
                   .ExecuteNonQueryAsync().caf()).caf();
      if(advanced == 1) return true;

      var admittedAsFirstContact = await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        INSERT INTO {_tables.InboxDeliveryStreamAdmissions}
                                    ({Admissions.SenderEndpointId}, {Admissions.LastAdmittedSequenceNumber})
                             SELECT @{Admissions.SenderEndpointId}, @{Admissions.LastAdmittedSequenceNumber}
                        WHERE @PredecessorSequenceNumber = 0
                          AND NOT EXISTS (SELECT 1 FROM {_tables.InboxDeliveryStreamAdmissions} WHERE {Admissions.SenderEndpointId} = @{Admissions.SenderEndpointId})

                        """)
                   .AddParameter(Admissions.SenderEndpointId, position.SenderEndpointId.Value)
                   .AddParameter(Admissions.LastAdmittedSequenceNumber, position.SequenceNumber)
                   .AddParameter("PredecessorSequenceNumber", position.PredecessorSequenceNumber)
                   .ExecuteNonQueryAsync().caf()).caf();
      return admittedAsFirstContact == 1;
   }

   async Task<long> GetLastAdmittedSequenceNumberAsync(DeliveryStreamPosition position) =>
      await _connectionFactory.UseCommandAsync(
         async command => (long)(await command
                   .SetCommandText(
                       $"""

                        SELECT COALESCE(MAX({Admissions.LastAdmittedSequenceNumber}), 0)
                        FROM {_tables.InboxDeliveryStreamAdmissions}
                        WHERE {Admissions.SenderEndpointId} = @{Admissions.SenderEndpointId}

                        """)
                   .AddParameter(Admissions.SenderEndpointId, position.SenderEndpointId.Value)
                   .ExecuteScalarAsync().caf())!).caf();

   //UPDLOCK+ROWLOCK take the row lock the claim IS - held to the end of the caller's ambient handling transaction - and
   //READPAST makes a row another live handling transaction has claimed report unclaimable instead of blocking on it.
   public async Task<bool> TryClaimForHandlingAsync(TessageId tessageId) =>
      await _connectionFactory.UseCommandAsync(
         async command => null != await command
                   .SetCommandText(
                       $"""

                        SELECT 1 FROM {_tables.InboxTessages} WITH (UPDLOCK, ROWLOCK, READPAST)
                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                            AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}

                        """)
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .ExecuteScalarAsync().caf()).caf();

   public async Task<IReadOnlyList<ITessagingSqlLayer.UnHandledTessage>> GetUnHandledTessagesAsync()
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var raw = await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var rows = new List<(TessageId TessageId, int TypeId, string Body)>();

            command.SetCommandText(
               $"""

                SELECT {TessageTable.TessageId}, {TessageTable.TypeId}, {TessageTable.Body}
                FROM {_tables.InboxTessages}
                WHERE {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}
                ORDER BY {TessageTable.GeneratedId} -- Admission order: per pair this is stream order, so recovery re-establishes in-order handling.

                """);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt32(1),
                         reader.GetString(2)));
            }

            return rows;
         }).caf();

      return [..raw.Select(row => new ITessagingSqlLayer.UnHandledTessage(
                              tessageId: row.TessageId,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              serializedTessage: row.Body))];
   }

   public async Task<int> MarkAsSucceededAsync(TessageId tessageId)
   {
      return await _connectionFactory.UseCommandAsync(
         async command =>
            await command
              .SetCommandText(
                  $"""

                   UPDATE {_tables.InboxTessages}
                       SET {TessageTable.Status} = {(int)InboxTessageStatus.Succeeded}
                   WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                       AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task<int> RecordExceptionAsync(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType)
   {
      return await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.InboxTessages}
                            SET {TessageTable.ExceptionCount} = {TessageTable.ExceptionCount} + 1,
                                {TessageTable.ExceptionType} = @{TessageTable.ExceptionType},
                                {TessageTable.ExceptionStackTrace} = @{TessageTable.ExceptionStackTrace},
                                {TessageTable.ExceptionTessage} = @{TessageTable.ExceptionTessage}

                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}

                        """)
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .AddNVarcharMaxParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddNVarcharMaxParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddNVarcharParameter(TessageTable.ExceptionType, 500, exceptionType)
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task<int> MarkAsFailedAsync(TessageId tessageId)
   {
      return await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.InboxTessages}
                            SET {TessageTable.Status} = {(int)InboxTessageStatus.Failed}
                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                            AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}
                        """)
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
