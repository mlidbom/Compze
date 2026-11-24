using Compze.Abstractions;
using Compze.Contracts;
using Compze.Sql.Common._internal;
using Compze.Sql.PostgreSql._internal;
using Compze.Internals.SystemCE;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Npgsql;
using NpgsqlTypes;
using ReadOrder = Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions.ReadOrder;
using Tevent = Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.PostgreSql._private;

//Performance: explore PgSql alternatives to commented out MSSql hints throughout the sql layer.
partial class PgSqlTeventStoreSqlLayer
{
   ///<summary>Inserts <paramref name="tevents"/>, each with an INSERT of its own, and an UPDATE of its own when it has no read order yet.</summary>
   ///<remarks>One statement per command: Npgsql 9 sends the second and later statements of a synchronous command, and flushes the whole<br/>
   /// command, from the thread pool, while the calling thread waits for the answer. With every pool thread busy, as under many units of<br/>
   /// work at once, the server receives nothing until the pool adds a thread.</remarks>
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
               Insert(connection, data, internedTypeId);
            }
            catch(PostgresException e) when(SqlExceptions.PgSql.IsPrimaryKeyViolation(e))
            {
               //todo: Make sure we have test coverage for this.
               throw new TeventDuplicateKeyException(e);
            }

            if(data.StorageInformation.ReadOrder == null) TakeItsInsertionOrderAsItsReadOrder(connection, data.TeventId);
         }
      });
   }

   ///<summary>Inserts <paramref name="data"/>, with a temporary read order when it has none yet.</summary>
   static void Insert(ICompzeNpgsqlConnection connection, TeventDataRow data, int internedTypeId) =>
      connection.UseCommand(
         command => command.SetCommandText(
                               $"""

                                INSERT INTO {Tevent.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                (       {Tevent.TaggregateId},  {Tevent.InsertedVersion},  {Tevent.EffectiveVersion},       {Tevent.ReadOrder},                            {Tevent.TeventType},  {Tevent.TeventId},  {Tevent.UtcTimeStamp},  {Tevent.Tevent},  {Tevent.TargetTevent}, {Tevent.RefactoringType}) 
                                VALUES(@{Tevent.TaggregateId}, @{Tevent.InsertedVersion}, @{Tevent.EffectiveVersion}, cast(@{Tevent.ReadOrder} as {Tevent.ReadOrderType}), @{Tevent.TeventType}, @{Tevent.TeventId}, @{Tevent.UtcTimeStamp}, @{Tevent.Tevent}, @{Tevent.TargetTevent},@{Tevent.RefactoringType});

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

   ///<summary>Replaces the temporary read order of the tevent <paramref name="teventId"/>, just inserted, with its insertion order.</summary>
   static void TakeItsInsertionOrderAsItsReadOrder(ICompzeNpgsqlConnection connection, TessageId teventId) =>
      connection.UseCommand(
         command => command.SetCommandText(
                               $"""

                                UPDATE {Tevent.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                                        SET {Tevent.ReadOrder} = cast({Tevent.InsertionOrder} as {Tevent.ReadOrderType})
                                        WHERE {Tevent.TeventId} = @{Tevent.TeventId};

                                """)
                           .AddParameter(Tevent.TeventId, teventId.Value)
                           .PrepareStatement()
                           .ExecuteNonQuery());

   ///<summary>Sets the effective version of each tevent in <paramref name="versions"/>, in one statement: see <see cref="InsertSingleTaggregateTevents"/> for why one.</summary>
   public void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions) =>
      _connectionManager.UseCommand(
         command => command.SetCommandText(
                               $"""

                                UPDATE {Tevent.TableName}
                                        SET {Tevent.EffectiveVersion} = version.{Tevent.EffectiveVersion}
                                        FROM unnest(@{Tevent.TeventId}, @{Tevent.EffectiveVersion}) AS version({Tevent.TeventId}, {Tevent.EffectiveVersion})
                                        WHERE {Tevent.TableName}.{Tevent.TeventId} = version.{Tevent.TeventId};

                                """)
                           .AddParameter(Tevent.TeventId, NpgsqlDbType.Array | NpgsqlDbType.Uuid, versions.Select(version => version.TeventId.Value).ToArray())
                           .AddParameter(Tevent.EffectiveVersion, NpgsqlDbType.Array | NpgsqlDbType.Integer, versions.Select(version => version.EffectiveVersion).ToArray())
                           .ExecuteNonQuery());

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