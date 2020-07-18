using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MySql.SystemExtensions;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.MySql.EventStore
{
    partial class MySqlEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly MySqlEventStoreConnectionManager _connectionManager;

        public MySqlEventStorePersistenceLayer(MySqlEventStoreConnectionManager connectionManager) => _connectionManager = connectionManager;

        static string CreateSelectClause(bool takeWriteLock) => InternalSelect(takeWriteLock: takeWriteLock);

        static string CreateLockHint(bool takeWriteLock) => takeWriteLock ? "FOR UPDATE" : "";
        // ReSharper disable once UnusedParameter.Local
        static string InternalSelect(bool takeWriteLock, int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            //todo: Ensure that READCOMMITTED is truly sane here. If so add a comment describing why and why using it is a good idea.
            //Urgent: Find mysql equivalents
            //var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";
            var lockHint = "";

            return $@"
SELECT {topClause} 
{Event.EventType}, {Event.Event}, {Event.AggregateId}, {Event.EffectiveVersion}, {Event.EventId}, {Event.UtcTimeStamp}, {Event.InsertionOrder}, {Event.TargetEvent}, {Event.RefactoringType}, {Event.InsertedVersion}, cast({Event.ReadOrder} as char(39))
FROM {Event.TableName} {lockHint} ";
        }

        static EventDataRow ReadDataRow(MySqlDataReader eventReader)
        {
            return new EventDataRow(
                eventType: eventReader.GetGuid(0),
                eventJson: eventReader.GetString(1),
                eventId: eventReader.GetGuid(4),
                aggregateVersion: eventReader.GetInt32(3),
                aggregateId: eventReader.GetGuid(2),
                //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
                utcTimeStamp: DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
                storageInformation: new AggregateEventStorageInformation()
                                        {
                                            ReadOrder = ReadOrder.Parse(eventReader.GetString(10)),
                                            InsertedVersion = eventReader.GetInt32(9),
                                            EffectiveVersion = eventReader.GetInt32(3),
                                            RefactoringInformation = (eventReader[7] as Guid?, eventReader[8] as sbyte?)switch
                                            {
                                                (null, null) => null,
                                                (Guid targetEvent, sbyte type) => new AggregateEventRefactoringInformation(targetEvent, (AggregateEventRefactoringType)type),
                                                _ => throw new Exception("Should not be possible to get here")
                                            }
                                        }
            );
        }

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
            _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                          command => command.SetCommandText($@"
{CreateSelectClause(takeWriteLock)} 
WHERE {Event.AggregateId} = @{Event.AggregateId}
    AND {Event.InsertedVersion} >= @CachedVersion
    AND {Event.EffectiveVersion} > 0
ORDER BY {Event.ReadOrder} ASC
{CreateLockHint(takeWriteLock)}")
                                                            .AddParameter(Event.AggregateId, aggregateId)
                                                            .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                            .ExecuteReaderAndSelect(ReadDataRow)
                                                            .SkipWhile(row => row.StorageInformation.InsertedVersion <= startAfterInsertedVersion)
                                                            .ToList());

        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
        {
            ReadOrder lastReadEventReadOrder = default;
            int fetchedInThisBatch;
            do
            {
                var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                                command =>
                                                                {
                                                                    var commandText = $@"
{CreateSelectClause(takeWriteLock: false)} 
WHERE {Event.ReadOrder}  > CAST(@{Event.ReadOrder} AS {Event.ReadOrderType})
    AND {Event.EffectiveVersion} > 0
ORDER BY {Event.ReadOrder} ASC
LIMIT {batchSize}";
                                                                    return command.SetCommandText(commandText)
                                                                                  .AddParameter(Event.ReadOrder, MySqlDbType.String, lastReadEventReadOrder.ToString())
                                                                                  .ExecuteReaderAndSelect(ReadDataRow)
                                                                                  .ToList();
                                                                });
                if(historyData.Any())
                {
                    lastReadEventReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value;
                }

                //We do not yield while reading from the reader since that may cause code to run that will cause another sql call into the same connection. Something that throws an exception unless you use an unusual and non-recommended connection string setting.
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
                                                 action: command => command.SetCommandText($@"
SELECT {Event.AggregateId}, {Event.EventType} 
FROM {Event.TableName} 
WHERE {Event.EffectiveVersion} = 1 
ORDER BY {Event.ReadOrder} ASC")
                                                                           .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: reader.GetGuid(0), typeId: reader.GetGuid(1))));
        }
    }
}
