using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Sql.Common;
using Compze.Sql.Common.EventStore.Abstractions;
using Compze.Sql.Sqlite;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Microsoft.Data.Sqlite;
using ReadOrder = Compze.Sql.Common.EventStore.Abstractions.ReadOrder;
using Event = Compze.Sql.Common.EventStore.EventTableSchemaStrings;

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

                                         INSERT INTO {Event.TableName}
                                         (       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},  {Event.ReadOrderIntegerPart},  {Event.ReadOrderFractionPart},  {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
                                         VALUES(@{Event.AggregateId}, @{Event.InsertedVersion}, @{Event.EffectiveVersion}, @{Event.ReadOrderIntegerPart}, @{Event.ReadOrderFractionPart}, @{Event.EventType}, @{Event.EventId}, @{Event.UtcTimeStamp}, @{Event.Event}, @{Event.TargetEvent},@{Event.RefactoringType});


                                         {(data.StorageInformation.ReadOrder != null ? "" : $"""

                                                                                             UPDATE {Event.TableName}
                                                                                             SET {Event.ReadOrderIntegerPart} = {Event.InsertionOrder},
                                                                                                 {Event.ReadOrderFractionPart} = 0
                                                                                             WHERE {Event.EventId} = @{Event.EventId};

                                                                                             """)}

                                         """)
                                    .AddVarcharParameter(Event.AggregateId, 36, data.AggregateId.ToString())
                                    .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                    .AddVarcharParameter(Event.EventType, 36, data.EventType.ToString())
                                    .AddVarcharParameter(Event.EventId, 36, data.EventId.ToString())
                                    .AddDateTime2Parameter(Event.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddMediumTextParameter(Event.Event, data.EventJson)
                                    .AddParameter(Event.ReadOrderIntegerPart, (data.StorageInformation.ReadOrder ?? ReadOrder.Zero).IntegerPart)
                                    .AddParameter(Event.ReadOrderFractionPart, (data.StorageInformation.ReadOrder ?? ReadOrder.Zero).FractionPart)
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

                             SELECT  {Event.ReadOrderIntegerPart}, {Event.ReadOrderFractionPart},        
                                     (select {Event.ReadOrderIntegerPart} from {Event.TableName} e1 
                                      where e1.{Event.ReadOrderIntegerPart} < {Event.TableName}.{Event.ReadOrderIntegerPart}
                                         OR (e1.{Event.ReadOrderIntegerPart} = {Event.TableName}.{Event.ReadOrderIntegerPart} AND e1.{Event.ReadOrderFractionPart} < {Event.TableName}.{Event.ReadOrderFractionPart})
                                      order by e1.{Event.ReadOrderIntegerPart} desc, e1.{Event.ReadOrderFractionPart} desc limit 1) PreviousIntegerPart,
                                     (select {Event.ReadOrderFractionPart} from {Event.TableName} e1 
                                      where e1.{Event.ReadOrderIntegerPart} < {Event.TableName}.{Event.ReadOrderIntegerPart}
                                         OR (e1.{Event.ReadOrderIntegerPart} = {Event.TableName}.{Event.ReadOrderIntegerPart} AND e1.{Event.ReadOrderFractionPart} < {Event.TableName}.{Event.ReadOrderFractionPart})
                                      order by e1.{Event.ReadOrderIntegerPart} desc, e1.{Event.ReadOrderFractionPart} desc limit 1) PreviousFractionPart,
                                     (select {Event.ReadOrderIntegerPart} from {Event.TableName} e1 
                                      where e1.{Event.ReadOrderIntegerPart} > {Event.TableName}.{Event.ReadOrderIntegerPart}
                                         OR (e1.{Event.ReadOrderIntegerPart} = {Event.TableName}.{Event.ReadOrderIntegerPart} AND e1.{Event.ReadOrderFractionPart} > {Event.TableName}.{Event.ReadOrderFractionPart})
                                      order by e1.{Event.ReadOrderIntegerPart}, e1.{Event.ReadOrderFractionPart} limit 1) NextIntegerPart,
                                     (select {Event.ReadOrderFractionPart} from {Event.TableName} e1 
                                      where e1.{Event.ReadOrderIntegerPart} > {Event.TableName}.{Event.ReadOrderIntegerPart}
                                         OR (e1.{Event.ReadOrderIntegerPart} = {Event.TableName}.{Event.ReadOrderIntegerPart} AND e1.{Event.ReadOrderFractionPart} > {Event.TableName}.{Event.ReadOrderFractionPart})
                                      order by e1.{Event.ReadOrderIntegerPart}, e1.{Event.ReadOrderFractionPart} limit 1) NextFractionPart
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

            var effectiveReadOrder = ReadOrder.FromParts(reader.GetInt64(0), reader.GetInt64(1));
            var previousEventReadOrder = reader.IsDBNull(2) ? null : new ReadOrder?(ReadOrder.FromParts(reader.GetInt64(2), reader.GetInt64(3)));
            var nextEventReadOrder = reader.IsDBNull(4) ? null : new ReadOrder?(ReadOrder.FromParts(reader.GetInt64(4), reader.GetInt64(5)));
            neighborhood = new EventNeighborhood(effectiveReadOrder: effectiveReadOrder,
                                                 previousEventReadOrder: previousEventReadOrder,
                                                 nextEventReadOrder: nextEventReadOrder);
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
