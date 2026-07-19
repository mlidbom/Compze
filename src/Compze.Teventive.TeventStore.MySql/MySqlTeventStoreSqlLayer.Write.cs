using Compze.Abstractions.Public;
using Compze.Contracts;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.SystemCE;
using Compze.Tessaging.Abstractions;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using MySqlConnector;
using ReadOrder = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions.ReadOrder;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.MySql;

//Performance: explore MySql alternatives to commented out MSSql hints throughout the sql layer.
partial class MySqlTeventStoreSqlLayer
{
   public void InsertSingleTaggregateTevents(IReadOnlyList<TeventDataRow> tevents)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var rows = tevents.Select(data => (data, internedTypeId: _typeIdInterner.GetOrInternId(data.TeventType))).ToList();
      _connectionManager.UseConnection(connection =>
      {
         foreach(var (data, internedTypeId) in rows)
         {
            try
            {
               connection.UseCommand(
                  command => command.SetCommandText(
                                        $"""

                                         INSERT {Tevent.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                         (       {Tevent.TaggregateId},  {Tevent.InsertedVersion},  {Tevent.EffectiveVersion},  {Tevent.ReadOrder},  {Tevent.TeventType},  {Tevent.TeventId},  {Tevent.UtcTimeStamp},  {Tevent.Tevent},  {Tevent.TargetTevent}, {Tevent.RefactoringType}) 
                                         VALUES(@{Tevent.TaggregateId}, @{Tevent.InsertedVersion}, @{Tevent.EffectiveVersion}, @{Tevent.ReadOrder}, @{Tevent.TeventType}, @{Tevent.TeventId}, @{Tevent.UtcTimeStamp}, @{Tevent.Tevent}, @{Tevent.TargetTevent},@{Tevent.RefactoringType});

                                         {(data.StorageInformation.ReadOrder != null ? "" : $"""

                                                                                             UPDATE {Tevent.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                                                                             SET {Tevent.ReadOrder} = cast({Tevent.InsertionOrder} as {Tevent.ReadOrderType})
                                                                                             WHERE {Tevent.TeventId} = @{Tevent.TeventId};

                                                                                             """)}

                                         """)
                                    .AddParameter(Tevent.TaggregateId, data.TaggregateId.Value)
                                    .AddParameter(Tevent.InsertedVersion, data.StorageInformation.InsertedVersion)
                                    .AddParameter(Tevent.TeventType, internedTypeId)
                                    .AddParameter(Tevent.TeventId, data.TeventId.Value)
                                    .AddDateTime2Parameter(Tevent.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddMediumTextParameter(Tevent.Tevent, data.TeventJson)

                                    .AddParameter(Tevent.ReadOrder, MySqlDbType.VarChar, data.StorageInformation.ReadOrder?.ToString() ?? ReadOrder.NextTemporaryPlaceholder().ToString())
                                    .AddParameter(Tevent.EffectiveVersion, MySqlDbType.Int32, data.StorageInformation.EffectiveVersion)
                                    .AddNullableParameter(Tevent.TargetTevent, MySqlDbType.VarChar, data.StorageInformation.RefactoringInformation?.TargetTevent.Value)
                                    .AddNullableParameter(Tevent.RefactoringType, MySqlDbType.Byte, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                    .ExecuteNonQuery());
            }
            catch(MySqlException e) when (SqlExceptions.MySql.IsPrimaryKeyViolation(e))
            {
               //todo: Make sure we have test coverage for this.
               throw new TeventDuplicateKeyException(e);
            }
         }
      });
   }

   public void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions)
   {
      var commandText = versions.Select((spec, _) =>
                                           $"UPDATE {Tevent.TableName} SET {Tevent.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Tevent.TeventId} = '{spec.TeventId}';").Join(Environment.NewLine);

      _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

   }

   public TeventNeighborhood LoadTeventNeighborHood(TessageId teventId)
   {
      //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
      const string lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

      var selectStatement = $"""

                             SELECT  CAST({Tevent.ReadOrder} AS char(39)),        
                                     (select cast({Tevent.ReadOrder} as char(39)) from {Tevent.TableName} e1 where e1.{Tevent.ReadOrder} < {Tevent.TableName}.{Tevent.ReadOrder} order by {Tevent.ReadOrder} desc limit 1) PreviousReadOrder,
                                     (select cast({Tevent.ReadOrder} as char(39)) from {Tevent.TableName} e1 where e1.{Tevent.ReadOrder} > {Tevent.TableName}.{Tevent.ReadOrder} order by {Tevent.ReadOrder} limit 1) NextReadOrder
                             FROM    {Tevent.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
                             where {Tevent.TeventId} = @{Tevent.TeventId}
                             """;

      TeventNeighborhood? neighborhood = null;

      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText = selectStatement;

            command.Parameters.Add(new MySqlParameter(Tevent.TeventId, MySqlDbType.Guid) { Value = teventId.Value });
            using var reader = command.ExecuteReader();
            reader.Read();

            var effectiveReadOrder = reader.GetString(0).ReplaceOrdinal(",", ".");
            var previousTeventReadOrder = (reader[1] as string)?.ReplaceOrdinal(",", ".");
            var nextTeventReadOrder = (reader[2] as string)?.ReplaceOrdinal(",", ".");
            neighborhood = new TeventNeighborhood(effectiveReadOrder: ReadOrder.Parse(effectiveReadOrder),
                                                 previousTeventReadOrder: previousTeventReadOrder == null ? null : new ReadOrder?(ReadOrder.Parse(previousTeventReadOrder)),
                                                 nextTeventReadOrder: nextTeventReadOrder == null ? null : new ReadOrder?(ReadOrder.Parse(nextTeventReadOrder)));
         });

      return neighborhood._assert().NotNull();
   }

   public void DeleteTaggregate(TaggregateId taggregateId)
   {
      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText +=
               $"DELETE FROM {Tevent.TableName} /*With(ROWLOCK)*/ WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId};";
            command.Parameters.Add(new MySqlParameter(Tevent.TaggregateId, MySqlDbType.Guid) { Value = taggregateId.Value });
            command.ExecuteNonQuery()._assert(rowsAffected => rowsAffected > 0);
         });
   }
}