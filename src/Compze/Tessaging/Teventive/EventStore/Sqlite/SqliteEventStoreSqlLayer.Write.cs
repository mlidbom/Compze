using Compze.Sql.Common;
using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Microsoft.Data.Sqlite;
using ReadOrder = Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions.ReadOrder;
using Event = Compze.Tessaging.Teventive.EventStore.EventTableSchemaStrings;
using Lock = Compze.Tessaging.Teventive.EventStore.AggregateLockTableSchemaStrings;

namespace Compze.Tessaging.Teventive.EventStore.Sqlite;

partial class SqliteEventStoreSqlLayer
{
   public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events)
   {
      _connectionManager.UseConnection(connection =>
      {
         foreach(var data in events)
         {
            try
            {
               connection.UseCommand(
                  command => command.SetCommandText(
                                        $"""

                                         {(data.AggregateVersion > 1 ? "" : $"INSERT OR IGNORE INTO {Lock.TableName}({Lock.AggregateId}) VALUES(@{Lock.AggregateId});")}

                                         INSERT INTO {Event.TableName}
                                         (       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},  {Event.ReadOrder},  {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
                                         VALUES(@{Event.AggregateId}, @{Event.InsertedVersion}, @{Event.EffectiveVersion}, @{Event.ReadOrder}, @{Event.EventType}, @{Event.EventId}, @{Event.UtcTimeStamp}, @{Event.Event}, @{Event.TargetEvent},@{Event.RefactoringType});


                                         {(data.StorageInformation.ReadOrder != null ? "" : $"""

                                                                                             UPDATE {Event.TableName}
                                                                                             SET {Event.ReadOrder} = printf('%d.%019d', {Event.InsertionOrder}, 0)
                                                                                             WHERE {Event.EventId} = @{Event.EventId};

                                                                                             """)}

                                         """)
                                    .AddVarcharParameter(Event.AggregateId, 36, data.AggregateId.ToString())
                                    .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                    .AddVarcharParameter(Event.EventType, 36, data.EventType.ToString())
                                    .AddVarcharParameter(Event.EventId, 36, data.EventId.ToString())
                                    .AddDateTime2Parameter(Event.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddMediumTextParameter(Event.Event, data.EventJson)
                                    .AddVarcharParameter(Event.ReadOrder, 50, data.StorageInformation.ReadOrder?.ToString() ?? new ReadOrder().ToString())
                                    .AddParameter(Event.EffectiveVersion, data.StorageInformation.EffectiveVersion)
                                    .AddNullableParameter(Event.TargetEvent, SqliteType.Text, data.StorageInformation.RefactoringInformation?.TargetEvent.ToString())
                                    .AddNullableParameter(Event.RefactoringType, SqliteType.Integer, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (int?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                    .ExecuteNonQuery());
            }
            catch(SqliteException e) when(SqlExceptions.Sqlite.IsPrimaryKeyViolation(e))
            {
               throw new EventDuplicateKeyException(e);
            }
         }
      });
   }

   public void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions)
   {
      var commandText = versions.Select((spec, _) =>
                                           $"UPDATE {Event.TableName} SET {Event.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Event.EventId} = '{spec.EventId}';").Join(Environment.NewLine);

      _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));
   }

   public EventNeighborhood LoadEventNeighborHood(Guid eventId)
   {
      var selectStatement = $"""

                             SELECT  {Event.ReadOrder},        
                                     (select {Event.ReadOrder} from {Event.TableName} e1 where e1.{Event.ReadOrder} < {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} desc limit 1) PreviousReadOrder,
                                     (select {Event.ReadOrder} from {Event.TableName} e1 where e1.{Event.ReadOrder} > {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} limit 1) NextReadOrder
                             FROM    {Event.TableName} 
                             where {Event.EventId} = @{Event.EventId}
                             """;

      EventNeighborhood? neighborhood = null;

      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText = selectStatement;
            command.AddVarcharParameter(Event.EventId, 36, eventId.ToString());
            using var reader = command.ExecuteReader();
            reader.Read();

            var effectiveReadOrder = reader.GetString(0);
            var previousEventReadOrder = reader.IsDBNull(1) ? null : reader.GetString(1);
            var nextEventReadOrder = reader.IsDBNull(2) ? null : reader.GetString(2);
            neighborhood = new EventNeighborhood(effectiveReadOrder: ReadOrder.Parse(effectiveReadOrder),
                                                 previousEventReadOrder: previousEventReadOrder == null ? null : new ReadOrder?(ReadOrder.Parse(previousEventReadOrder)),
                                                 nextEventReadOrder: nextEventReadOrder == null ? null : new ReadOrder?(ReadOrder.Parse(nextEventReadOrder)));
         });

      return Assert.Result.NotNull(neighborhood).then(neighborhood);
   }

   public void DeleteAggregate(Guid aggregateId)
   {
      _connectionManager.UseCommand(
         command =>
         {
            command.SetCommandText($"DELETE FROM {Event.TableName} WHERE {Event.AggregateId} = @{Event.AggregateId};")
                   .AddVarcharParameter(Event.AggregateId, 36, aggregateId.ToString())
                   .ExecuteNonQuery();
         });
   }
}
