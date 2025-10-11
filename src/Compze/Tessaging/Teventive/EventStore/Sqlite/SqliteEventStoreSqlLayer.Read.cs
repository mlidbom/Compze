using Compze.Sql.Common;
using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;
using Microsoft.Data.Sqlite;
using Event = Compze.Tessaging.Teventive.EventStore.EventTableSchemaStrings;
using Lock = Compze.Tessaging.Teventive.EventStore.AggregateLockTableSchemaStrings;

namespace Compze.Tessaging.Teventive.EventStore.Sqlite;

partial class SqliteEventStoreSqlLayer(SqliteEventStoreConnectionManager connectionManager) : IEventStoreSqlLayer
{
   readonly SqliteEventStoreConnectionManager _connectionManager = connectionManager;

   static string CreateSelectClause() => InternalSelect();

   static string InternalSelect(int? top = null)
   {
      var topClause = top.HasValue ? $"LIMIT {top.Value} " : "";

      return $"""

              SELECT 
              {Event.EventType}, {Event.Event}, {Event.AggregateId}, {Event.EffectiveVersion}, {Event.EventId}, {Event.UtcTimeStamp}, {Event.InsertionOrder}, {Event.TargetEvent}, {Event.RefactoringType}, {Event.InsertedVersion}, {Event.ReadOrder} as CharReadOrder
              FROM {Event.TableName}
              {topClause}
              """;
   }

   static EventDataRow ReadDataRow(SqliteDataReader eventReader) => new(
      eventType: Guid.Parse(eventReader.GetString(0)),
      eventJson: eventReader.GetString(1),
      eventId: Guid.Parse(eventReader.GetString(4)),
      aggregateVersion: eventReader.GetInt32(3),
      aggregateId: Guid.Parse(eventReader.GetString(2)),
      // DateTime stored as Ticks (INTEGER) for full precision
      utcTimeStamp: new DateTime(eventReader.GetInt64(5), DateTimeKind.Utc),
      storageInformation: new AggregateEventStorageInformation
                          {
                             ReadOrder = ReadOrder.Parse(eventReader.GetString(10)),
                             InsertedVersion = eventReader.GetInt32(9),
                             EffectiveVersion = eventReader.GetInt32(3),
                             RefactoringInformation = (eventReader.IsDBNull(7) ? (Guid?)null : Guid.Parse(eventReader.GetString(7)), eventReader.IsDBNull(8) ? (int?)null : eventReader.GetInt32(8))switch
                             {
                                (null, null) => null,
                                (Guid targetEvent, int type) => new AggregateEventRefactoringInformation(targetEvent, (AggregateEventRefactoringType)type),
                                _ => throw new Exception("Should not be possible to get here")
                             }
                          }
   );

   public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
   {

      return _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                          command => command.SetCommandText($"""

                                                                             {CreateSelectClause()} 
                                                                             WHERE {Event.AggregateId} = @{Event.AggregateId}
                                                                                 AND {Event.InsertedVersion} > @CachedVersion
                                                                                 AND {Event.EffectiveVersion} > 0
                                                                             ORDER BY {Event.ReadOrder} ASC
                                                                             """)
                                                            .AddVarcharParameter(Event.AggregateId, 36, aggregateId.ToString())
                                                            .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                            .ExecuteReaderAndSelect(ReadDataRow)
                                                            .ToList());
   }

   public IEnumerable<EventDataRow> StreamEvents(int batchSize)
   {
      string lastReadEventReadOrder = ReadOrder.Zero.ToString();
      int fetchedInThisBatch;
      do
      {
         var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                         command => command.SetCommandText($"""

                                                                                            SELECT 
                                                                                            {Event.EventType}, {Event.Event}, {Event.AggregateId}, {Event.EffectiveVersion}, {Event.EventId}, {Event.UtcTimeStamp}, {Event.InsertionOrder}, {Event.TargetEvent}, {Event.RefactoringType}, {Event.InsertedVersion}, {Event.ReadOrder}
                                                                                            FROM {Event.TableName}
                                                                                            WHERE {Event.ReadOrder}  > @{Event.ReadOrder}
                                                                                                AND {Event.EffectiveVersion} > 0
                                                                                            ORDER BY {Event.ReadOrder} ASC
                                                                                            LIMIT {batchSize}
                                                                                            """)
                                                                           .AddVarcharParameter(Event.ReadOrder, 50, lastReadEventReadOrder)
                                                                           .ExecuteReaderAndSelect(ReadDataRow)
                                                                           .ToList());
         if(historyData.Any())
         {
            lastReadEventReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value.ToString();
         }

         foreach(var eventDataRow in historyData)
         {
            yield return eventDataRow;
         }

         fetchedInThisBatch = historyData.Count;
      } while(!(fetchedInThisBatch < batchSize));
   }

   public IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder()
   {
      return _connectionManager.UseCommand(suppressTransactionWarning: true,
                                           action: command => command.SetCommandText($"""

                                                                                      SELECT {Event.AggregateId}, {Event.EventType} 
                                                                                      FROM {Event.TableName} 
                                                                                      WHERE {Event.EffectiveVersion} = 1 
                                                                                      ORDER BY {Event.ReadOrder} ASC
                                                                                      """)
                                                                     .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: Guid.Parse(reader.GetString(0)), typeId: Guid.Parse(reader.GetString(1)))));
   }
}
