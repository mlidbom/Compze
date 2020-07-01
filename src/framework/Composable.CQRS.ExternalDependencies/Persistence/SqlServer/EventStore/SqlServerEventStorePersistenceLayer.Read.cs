using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;

namespace Composable.Persistence.SqlServer.EventStore
{
    partial class SqlServerEventStorePersistenceLayer : IEventStorePersistenceLayer
    {
        readonly SqlServerEventStoreConnectionManager _connectionManager;

        public SqlServerEventStorePersistenceLayer(SqlServerEventStoreConnectionManager connectionManager) => _connectionManager = connectionManager;

        static string CreateSelectClause(bool takeWriteLock) => InternalSelect(takeWriteLock: takeWriteLock);
        static string CreateSelectTopClause(int top, bool takeWriteLock) => InternalSelect(top: top, takeWriteLock: takeWriteLock);

        static string InternalSelect(bool takeWriteLock, int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            //todo: Ensure that READCOMMITTED is truly sane here. If so add a comment describing why and why using it is a good idea.
            var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";

            return $@"
SELECT {topClause} 
    {SqlServerEventTable.Columns.EventType}, 
    {SqlServerEventTable.Columns.Event}, 
    {SqlServerEventTable.Columns.AggregateId}, 
    {SqlServerEventTable.Columns.EffectiveVersion}, 
    {SqlServerEventTable.Columns.EventId}, 
    {SqlServerEventTable.Columns.UtcTimeStamp}, 
    {SqlServerEventTable.Columns.InsertionOrder}, 
    {SqlServerEventTable.Columns.InsertAfter}, 
    {SqlServerEventTable.Columns.InsertBefore}, 
    {SqlServerEventTable.Columns.Replaces}, 
    {SqlServerEventTable.Columns.InsertedVersion},
    {SqlServerEventTable.Columns.EffectiveReadOrder}
FROM {SqlServerEventTable.Name} {lockHint} ";
        }

        static EventDataRow ReadDataRow(SqlDataReader eventReader) => new EventDataRow(
            eventType: eventReader.GetGuid(0),
            eventJson: eventReader.GetString(1),
            eventId: eventReader.GetGuid(4),
            aggregateVersion: eventReader.GetInt32(3),
            aggregateId: eventReader.GetGuid(2),
            //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
            utcTimeStamp: DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
            refactoringInformation: new AggregateEventRefactoringInformation()
                                    {
                                        InsertedVersion = eventReader.GetInt32(10),
                                        ManualVersion = eventReader.GetInt32(3),
                                        InsertAfter = eventReader[7] as Guid?,
                                        InsertBefore = eventReader[8] as Guid?,
                                        Replaces = eventReader[9] as Guid?
                                    }
        );

        public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
            _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                          command => command.SetCommandText($@"
{CreateSelectClause(takeWriteLock)} 
WHERE {SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}
    AND {SqlServerEventTable.Columns.InsertedVersion} > @CachedVersion
ORDER BY {SqlServerEventTable.Columns.EffectiveReadOrder} ASC")
                                                            .AddParameter(SqlServerEventTable.Columns.AggregateId, aggregateId)
                                                            .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                            .ExecuteReaderAndSelect(ReadDataRow)
                                                            .Where(@this => (@this.RefactoringInformation.ManualVersion ?? 0) > 0)
                                                            .ToList());

        public IEnumerable<EventDataRow> StreamEvents(int batchSize)
        {
            SqlDecimal lastReadEventReadOrder = 0;
            int fetchedInThisBatch;
            do
            {
                var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                                command => command.SetCommandText($@"
{CreateSelectTopClause(batchSize, takeWriteLock: false)} 
WHERE {SqlServerEventTable.Columns.EffectiveReadOrder}  > @{SqlServerEventTable.Columns.EffectiveReadOrder}
{ReadSortOrder}")
                                                                                  .AddParameter(SqlServerEventTable.Columns.EffectiveReadOrder, SqlDbType.Decimal, lastReadEventReadOrder)
                                                                                  .ExecuteReaderAndSelect(reader =>
                                                                                   {
                                                                                       lastReadEventReadOrder = reader.GetSqlDecimal(11);
                                                                                       return ReadDataRow(reader);
                                                                                   })
                                                                                  .ToList());

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
SELECT {SqlServerEventTable.Columns.AggregateId}, {SqlServerEventTable.Columns.EventType} 
FROM {SqlServerEventTable.Name} 
WHERE {SqlServerEventTable.Columns.EffectiveVersion} = 1 AND {SqlServerEventTable.Columns.EffectiveReadOrder} > 0 {ReadSortOrder}")
                                                                           .ExecuteReaderAndSelect(reader => new CreationEventRow(aggregateId: reader.GetGuid(0), typeId: reader.GetGuid(1))));
        }

        static string ReadSortOrder => $" ORDER BY {SqlServerEventTable.Columns.EffectiveReadOrder} ASC";
    }
}
