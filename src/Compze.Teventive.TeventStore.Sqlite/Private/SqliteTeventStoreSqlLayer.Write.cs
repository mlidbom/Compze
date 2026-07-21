using Compze.Abstractions.Public;
using Compze.Contracts;
using Compze.Internals.Sql.Common;
using Compze.Internals.SystemCE;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Microsoft.Data.Sqlite;
using ReadOrder = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions.ReadOrder;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;
using Compze.Internals.Sql.Sqlite.Internal;

namespace Compze.Teventive.TeventStore.Sqlite.Private;

partial class SqliteTeventStoreSqlLayer
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
               var readOrder = data.StorageInformation.ReadOrder ?? ReadOrder.NextTemporaryPlaceholder();
               connection.UseCommand(
                  command => command.SetCommandText(
                                        $"""

                                         INSERT INTO {Tevent.TableName}
                                         (       {Tevent.TaggregateId},  {Tevent.InsertedVersion},  {Tevent.EffectiveVersion},  {Tevent.ReadOrderIntegerPart},  {Tevent.ReadOrderFractionPart},  {Tevent.TeventType},  {Tevent.TeventId},  {Tevent.UtcTimeStamp},  {Tevent.Tevent},  {Tevent.TargetTevent}, {Tevent.RefactoringType}) 
                                         VALUES(@{Tevent.TaggregateId}, @{Tevent.InsertedVersion}, @{Tevent.EffectiveVersion}, @{Tevent.ReadOrderIntegerPart}, @{Tevent.ReadOrderFractionPart}, @{Tevent.TeventType}, @{Tevent.TeventId}, @{Tevent.UtcTimeStamp}, @{Tevent.Tevent}, @{Tevent.TargetTevent},@{Tevent.RefactoringType});


                                         {(data.StorageInformation.ReadOrder != null ? "" : $"""

                                                                                             UPDATE {Tevent.TableName}
                                                                                             SET {Tevent.ReadOrderIntegerPart} = {Tevent.InsertionOrder},
                                                                                                 {Tevent.ReadOrderFractionPart} = 0
                                                                                             WHERE {Tevent.TeventId} = @{Tevent.TeventId};

                                                                                             """)}

                                         """)
                                    .AddMediumTextParameter(Tevent.TaggregateId, data.TaggregateId.ToString())
                                    .AddParameter(Tevent.InsertedVersion, data.StorageInformation.InsertedVersion)
                                    .AddParameter(Tevent.TeventType, internedTypeId)
                                    .AddMediumTextParameter(Tevent.TeventId, data.TeventId.ToString())
                                    .AddDateTime2Parameter(Tevent.UtcTimeStamp, data.UtcTimeStamp)
                                    .AddMediumTextParameter(Tevent.Tevent, data.TeventJson)
                                    .AddParameter(Tevent.ReadOrderIntegerPart, readOrder.IntegerPart)
                                    .AddParameter(Tevent.ReadOrderFractionPart, readOrder.FractionPart)
                                    .AddParameter(Tevent.EffectiveVersion, data.StorageInformation.EffectiveVersion)
                                    .AddNullableParameter(Tevent.TargetTevent, SqliteType.Text, data.StorageInformation.RefactoringInformation?.TargetTevent.ToString())
                                    .AddNullableParameter(Tevent.RefactoringType, SqliteType.Integer, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (int?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                    .ExecuteNonQuery());
            }
            catch(SqliteException e) when(SqlExceptions.Sqlite.IsPrimaryKeyViolation(e))
            {
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
      var selectStatement = $"""

                             SELECT  {Tevent.ReadOrderIntegerPart}, {Tevent.ReadOrderFractionPart},        
                                     (select {Tevent.ReadOrderIntegerPart} from {Tevent.TableName} e1 
                                      where e1.{Tevent.ReadOrderIntegerPart} < {Tevent.TableName}.{Tevent.ReadOrderIntegerPart}
                                         OR (e1.{Tevent.ReadOrderIntegerPart} = {Tevent.TableName}.{Tevent.ReadOrderIntegerPart} AND e1.{Tevent.ReadOrderFractionPart} < {Tevent.TableName}.{Tevent.ReadOrderFractionPart})
                                      order by e1.{Tevent.ReadOrderIntegerPart} desc, e1.{Tevent.ReadOrderFractionPart} desc limit 1) PreviousIntegerPart,
                                     (select {Tevent.ReadOrderFractionPart} from {Tevent.TableName} e1 
                                      where e1.{Tevent.ReadOrderIntegerPart} < {Tevent.TableName}.{Tevent.ReadOrderIntegerPart}
                                         OR (e1.{Tevent.ReadOrderIntegerPart} = {Tevent.TableName}.{Tevent.ReadOrderIntegerPart} AND e1.{Tevent.ReadOrderFractionPart} < {Tevent.TableName}.{Tevent.ReadOrderFractionPart})
                                      order by e1.{Tevent.ReadOrderIntegerPart} desc, e1.{Tevent.ReadOrderFractionPart} desc limit 1) PreviousFractionPart,
                                     (select {Tevent.ReadOrderIntegerPart} from {Tevent.TableName} e1 
                                      where e1.{Tevent.ReadOrderIntegerPart} > {Tevent.TableName}.{Tevent.ReadOrderIntegerPart}
                                         OR (e1.{Tevent.ReadOrderIntegerPart} = {Tevent.TableName}.{Tevent.ReadOrderIntegerPart} AND e1.{Tevent.ReadOrderFractionPart} > {Tevent.TableName}.{Tevent.ReadOrderFractionPart})
                                      order by e1.{Tevent.ReadOrderIntegerPart}, e1.{Tevent.ReadOrderFractionPart} limit 1) NextIntegerPart,
                                     (select {Tevent.ReadOrderFractionPart} from {Tevent.TableName} e1 
                                      where e1.{Tevent.ReadOrderIntegerPart} > {Tevent.TableName}.{Tevent.ReadOrderIntegerPart}
                                         OR (e1.{Tevent.ReadOrderIntegerPart} = {Tevent.TableName}.{Tevent.ReadOrderIntegerPart} AND e1.{Tevent.ReadOrderFractionPart} > {Tevent.TableName}.{Tevent.ReadOrderFractionPart})
                                      order by e1.{Tevent.ReadOrderIntegerPart}, e1.{Tevent.ReadOrderFractionPart} limit 1) NextFractionPart
                             FROM    {Tevent.TableName} 
                             where {Tevent.TeventId} = @{Tevent.TeventId}
                             """;

      return _connectionManager.UseCommand(
         command =>
         {
            command.CommandText = selectStatement;
            command.AddMediumTextParameter(Tevent.TeventId, teventId.ToString());
            using var reader = command.ExecuteReader();
            reader.Read();

            var effectiveReadOrder = ReadOrder.FromParts(reader.GetInt64(0), reader.GetInt64(1));
            var previousTeventReadOrder = reader.IsDBNull(2) ? null : new ReadOrder?(ReadOrder.FromParts(reader.GetInt64(2), reader.GetInt64(3)));
            var nextTeventReadOrder = reader.IsDBNull(4) ? null : new ReadOrder?(ReadOrder.FromParts(reader.GetInt64(4), reader.GetInt64(5)));
            return new TeventNeighborhood(effectiveReadOrder: effectiveReadOrder,
                                                 previousTeventReadOrder: previousTeventReadOrder,
                                                 nextTeventReadOrder: nextTeventReadOrder);
         });
   }

   public void DeleteTaggregate(TaggregateId taggregateId)
   {
      _connectionManager.UseCommand(
         command =>
         {
            command.SetCommandText($"DELETE FROM {Tevent.TableName} WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId};")
                   .AddMediumTextParameter(Tevent.TaggregateId, taggregateId.ToString())
                   .ExecuteNonQuery()._assert(rowsAffected => rowsAffected > 0);
         });
   }
}
