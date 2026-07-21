using System.Data;
using Compze.Abstractions.Public;
using Compze.Contracts;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.SystemCE;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Microsoft.Data.SqlClient;
using ReadOrder = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions.ReadOrder;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.MicrosoftSql.Private;

partial class MsSqlTeventStoreSqlLayer
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

                                         INSERT {Tevent.TableName} With(READCOMMITTED, ROWLOCK) --We are inserting append-only data so using READCOMMITTED to lessen the risk of lock problems should be perfectly safe.
                                         (       {Tevent.TaggregateId},  {Tevent.InsertedVersion},  {Tevent.EffectiveVersion},  {Tevent.ReadOrder},  {Tevent.TeventType},  {Tevent.TeventId},  {Tevent.UtcTimeStamp},  {Tevent.Tevent},  {Tevent.TargetTevent}, {Tevent.RefactoringType}) 
                                         VALUES(@{Tevent.TaggregateId}, @{Tevent.InsertedVersion}, @{Tevent.EffectiveVersion}, @{Tevent.ReadOrder}, @{Tevent.TeventType}, @{Tevent.TeventId}, @{Tevent.UtcTimeStamp}, @{Tevent.Tevent}, @{Tevent.TargetTevent},@{Tevent.RefactoringType})

                                         {(data.StorageInformation.ReadOrder != null ? "" : $"""

                                                                                             UPDATE {Tevent.TableName} With(READCOMMITTED, ROWLOCK)
                                                                                             SET {Tevent.ReadOrder} = cast({Tevent.InsertionOrder} as {Tevent.ReadOrderType})
                                                                                             WHERE {Tevent.TeventId} = @{Tevent.TeventId}

                                                                                             """)}

                                         """)
                                    .AddParameter(Tevent.TaggregateId, SqlDbType.UniqueIdentifier, data.TaggregateId.Value)
                                    .AddParameter(Tevent.InsertedVersion, data.StorageInformation.InsertedVersion)
                                    .AddParameter(Tevent.TeventType, internedTypeId)
                                    .AddParameter(Tevent.TeventId, data.TeventId.Value)
                                    .AddDateTime2Parameter(Tevent.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddNVarcharMaxParameter(Tevent.Tevent, data.TeventJson)

                                    .AddParameter(Tevent.ReadOrder, SqlDbType.Decimal, data.StorageInformation.ReadOrder?.ToSqlDecimal() ?? ReadOrder.NextTemporaryPlaceholder().ToSqlDecimal())
                                    .AddParameter(Tevent.EffectiveVersion, SqlDbType.Int, data.StorageInformation.EffectiveVersion)
                                    .AddNullableParameter(Tevent.TargetTevent, SqlDbType.UniqueIdentifier, data.StorageInformation.RefactoringInformation?.TargetTevent.Value)
                                    .AddNullableParameter(Tevent.RefactoringType, SqlDbType.TinyInt, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                    .ExecuteNonQuery());
            }
            catch(SqlException e) when(SqlExceptions.MsSql.IsPrimaryKeyViolation(e))
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
                                           $"UPDATE {Tevent.TableName} SET {Tevent.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Tevent.TeventId} = '{spec.TeventId}'").Join(Environment.NewLine);

      _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

   }

   public TeventNeighborhood LoadTeventNeighborHood(TessageId teventId)
   {


      const string lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

      var selectStatement = $"""

                             SELECT  {Tevent.ReadOrder},        
                                     (select top 1 {Tevent.ReadOrder} from {Tevent.TableName} e1 where e1.{Tevent.ReadOrder} < {Tevent.TableName}.{Tevent.ReadOrder} order by {Tevent.ReadOrder} desc) PreviousReadOrder,
                                     (select top 1 {Tevent.ReadOrder} from {Tevent.TableName} e1 where e1.{Tevent.ReadOrder} > {Tevent.TableName}.{Tevent.ReadOrder} order by {Tevent.ReadOrder}) NextReadOrder
                             FROM    {Tevent.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
                             where {Tevent.TeventId} = @{Tevent.TeventId}
                             """;

      TeventNeighborhood? neighborhood = null;

      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText = selectStatement;
            command.Parameters.Add(new SqlParameter(Tevent.TeventId, SqlDbType.UniqueIdentifier) {Value = teventId.Value});
            using var reader = command.ExecuteReader();
            reader.Read();

            var effectiveReadOrder = reader.GetSqlDecimal(0);
            var previousTeventReadOrder = reader.GetSqlDecimal(1);
            var nextTeventReadOrder = reader.GetSqlDecimal(2);
            neighborhood = new TeventNeighborhood(effectiveReadOrder: ReadOrder.FromSqlDecimal(effectiveReadOrder),
                                                 previousTeventReadOrder: previousTeventReadOrder.IsNull ? null : new ReadOrder?(ReadOrder.FromSqlDecimal(previousTeventReadOrder)),
                                                 nextTeventReadOrder: nextTeventReadOrder.IsNull ? null : new ReadOrder?(ReadOrder.FromSqlDecimal(nextTeventReadOrder)));
         });

      return neighborhood._assert().NotNull();
   }

   public void DeleteTaggregate(TaggregateId taggregateId)
   {
      _connectionManager.UseCommand(
         command =>
         {
            command.CommandText +=
               $"DELETE {Tevent.TableName} With(ROWLOCK) WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}";
            command.Parameters.Add(new SqlParameter(Tevent.TaggregateId, SqlDbType.UniqueIdentifier) {Value = taggregateId.Value});
            command.ExecuteNonQuery()._assert(rowsAffected => rowsAffected > 0);
         });
   }
}
