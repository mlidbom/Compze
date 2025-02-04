using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Contracts;
using Compze.Functional;
using Compze.Persistence.Common.AdoCE;
using Compze.Persistence.Common.EventStore;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.SystemCE;
using Npgsql;
using NpgsqlTypes;
using ReadOrder = Compze.Persistence.EventStore.PersistenceLayer.ReadOrder;
using Event = Compze.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Compze.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Compze.Persistence.PgSql.EventStore;

//Performance: explore PgSql alternatives to commented out MSSql hints throughout the persistence layer.
partial class PgSqlEventStorePersistenceLayer
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

                                         {(data.AggregateVersion > 1 ? "" : $"insert into {Lock.TableName}({Lock.AggregateId}) values(@{Lock.AggregateId});")}

                                         INSERT INTO {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                         (       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},       {Event.ReadOrder},                            {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
                                         VALUES(@{Event.AggregateId}, @{Event.InsertedVersion}, @{Event.EffectiveVersion}, cast(@{Event.ReadOrder} as {Event.ReadOrderType}), @{Event.EventType}, @{Event.EventId}, @{Event.UtcTimeStamp}, @{Event.Event}, @{Event.TargetEvent},@{Event.RefactoringType});

                                         {(data.StorageInformation.ReadOrder != null ? "" : $"""

                                                                                             UPDATE {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                                                                                     SET {Event.ReadOrder} = cast({Event.InsertionOrder} as {Event.ReadOrderType})
                                                                                                     WHERE {Event.EventId} = @{Event.EventId};

                                                                                             """)}


                                         """)
                                    .AddParameter(Event.AggregateId, data.AggregateId)
                                    .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                    .AddParameter(Event.EventType, data.EventType)
                                    .AddParameter(Event.EventId, data.EventId)
                                    .AddTimestampWithTimeZone(Event.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddMediumTextParameter(Event.Event, data.EventJson)
                                    .AddParameter(Event.ReadOrder, NpgsqlDbType.Varchar, data.StorageInformation.ReadOrder?.ToString() ?? new ReadOrder().ToString())
                                    .AddParameter(Event.EffectiveVersion, NpgsqlDbType.Integer, data.StorageInformation.EffectiveVersion)
                                    .AddNullableParameter(Event.TargetEvent, NpgsqlDbType.Varchar, data.StorageInformation.RefactoringInformation?.TargetEvent.ToString())
                                    .AddNullableParameter(Event.RefactoringType, NpgsqlDbType.Smallint, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                    .PrepareStatement()
                                    .ExecuteNonQuery());
            }
            catch(PostgresException e) when(SqlExceptions.PgSql.IsPrimaryKeyViolation(e))
            {
               //todo: Make sure we have test coverage for this.
               throw new EventDuplicateKeyException(e);
            }
         }
      });
   }

   public void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions)
   {
      var commandText = versions.Select((spec, _) =>
                                           $"UPDATE {Event.TableName} SET {Event.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Event.EventId} = '{spec.EventId}';").Join(Environment.NewLine);

      //We do not prepare here since this query will only ever be executed once.
      _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));
   }

   public EventNeighborhood LoadEventNeighborHood(Guid eventId)
   {
      const string lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

      var selectStatement = $"""

                             SELECT  cast({Event.ReadOrder} as varchar) as CharEffectiveOrder,        
                                     (select cast({Event.ReadOrder} as varchar) as CharEffectiveOrder from {Event.TableName} e1 where e1.{Event.ReadOrder} < {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} desc limit 1) PreviousReadOrder,
                                     (select cast({Event.ReadOrder} as varchar) as CharEffectiveOrder from {Event.TableName} e1 where e1.{Event.ReadOrder} > {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} limit 1) NextReadOrder
                             FROM    {Event.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
                             where {Event.EventId} = @{Event.EventId}
                             {CreateLockHint(takeWriteLock: true)}
                             """;

      EventNeighborhood? neighborhood = null;

      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText = selectStatement;

            command.AddParameter(Event.EventId, eventId);
            using var reader = command.PrepareStatement()
                                      .ExecuteReader();
            reader.Read();

            var effectiveReadOrder = reader.GetString(0).ReplaceInvariant(",", ".");
            var previousEventReadOrder = (reader[1] as string)?.ReplaceInvariant(",", ".");
            var nextEventReadOrder = (reader[2] as string)?.ReplaceInvariant(",", ".");
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
            command.SetCommandText($"DELETE FROM {Event.TableName} /*With(ROWLOCK)*/ WHERE {Event.AggregateId} = @{Event.AggregateId};")
                   .AddParameter(Event.AggregateId, aggregateId)
                   .PrepareStatement()
                   .ExecuteNonQuery();
         });
   }
}