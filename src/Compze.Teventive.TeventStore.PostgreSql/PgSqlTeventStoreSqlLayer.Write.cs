using Compze.Abstractions.Public;
using Compze.Contracts;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.SystemCE;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Npgsql;
using NpgsqlTypes;
using ReadOrder = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions.ReadOrder;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.PostgreSql;

//Performance: explore PgSql alternatives to commented out MSSql hints throughout the sql layer.
partial class PgSqlTeventStoreSqlLayer
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

                                         INSERT INTO {Tevent.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                         (       {Tevent.TaggregateId},  {Tevent.InsertedVersion},  {Tevent.EffectiveVersion},       {Tevent.ReadOrder},                            {Tevent.TeventType},  {Tevent.TeventId},  {Tevent.UtcTimeStamp},  {Tevent.Tevent},  {Tevent.TargetTevent}, {Tevent.RefactoringType}) 
                                         VALUES(@{Tevent.TaggregateId}, @{Tevent.InsertedVersion}, @{Tevent.EffectiveVersion}, cast(@{Tevent.ReadOrder} as {Tevent.ReadOrderType}), @{Tevent.TeventType}, @{Tevent.TeventId}, @{Tevent.UtcTimeStamp}, @{Tevent.Tevent}, @{Tevent.TargetTevent},@{Tevent.RefactoringType});

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
                                    .AddTimestampWithTimeZone(Tevent.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddMediumTextParameter(Tevent.Tevent, data.TeventJson)
                                    .AddParameter(Tevent.ReadOrder, NpgsqlDbType.Varchar, data.StorageInformation.ReadOrder?.ToString() ?? ReadOrder.NextTemporaryPlaceholder().ToString())
                                    .AddParameter(Tevent.EffectiveVersion, NpgsqlDbType.Integer, data.StorageInformation.EffectiveVersion)
                                    .AddNullableParameter(Tevent.TargetTevent, NpgsqlDbType.Uuid, data.StorageInformation.RefactoringInformation?.TargetTevent.Value)
                                    .AddNullableParameter(Tevent.RefactoringType, NpgsqlDbType.Smallint, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                    .PrepareStatement()
                                    .ExecuteNonQuery());
            }
            catch(PostgresException e) when(SqlExceptions.PgSql.IsPrimaryKeyViolation(e))
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

      //We do not prepare here since this tuery will only ever be executed once.
      _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));
   }

   public TeventNeighborhood LoadTeventNeighborHood(TessageId teventId)
   {
      const string lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

      var selectStatement = $"""

                             SELECT  cast({Tevent.ReadOrder} as varchar) as CharEffectiveOrder,        
                                     (select cast({Tevent.ReadOrder} as varchar) as CharEffectiveOrder from {Tevent.TableName} e1 where e1.{Tevent.ReadOrder} < {Tevent.TableName}.{Tevent.ReadOrder} order by {Tevent.ReadOrder} desc limit 1) PreviousReadOrder,
                                     (select cast({Tevent.ReadOrder} as varchar) as CharEffectiveOrder from {Tevent.TableName} e1 where e1.{Tevent.ReadOrder} > {Tevent.TableName}.{Tevent.ReadOrder} order by {Tevent.ReadOrder} limit 1) NextReadOrder
                             FROM    {Tevent.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
                             where {Tevent.TeventId} = @{Tevent.TeventId}
                             {CreateLockHint(takeWriteLock: true)}
                             """;

      TeventNeighborhood? neighborhood = null;

      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText = selectStatement;

            command.AddParameter(Tevent.TeventId, teventId.Value);
            using var reader = command.PrepareStatement()
                                      .ExecuteReader();
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
            command.SetCommandText($"DELETE FROM {Tevent.TableName} /*With(ROWLOCK)*/ WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId};")
                   .AddParameter(Tevent.TaggregateId, taggregateId.Value)
                   .PrepareStatement()
                   .ExecuteNonQuery()._assert(rowsAffected => rowsAffected > 0);
         });
   }
}