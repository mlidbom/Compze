using System.Transactions;
using Compze.Abstractions.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Npgsql;
using NpgsqlTypes;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.PostgreSql;

partial class PgSqlTeventStoreSqlLayer(PgSqlTeventStoreConnectionManager connectionManager, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : ITeventStoreSqlLayer
{
   readonly PgSqlTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   static string CreateSelectClause() => InternalSelect();

   static string CreateLockHint(bool takeWriteLock) => takeWriteLock ? "FOR UPDATE" : "";

   static string InternalSelect(int? top = null)
   {
      var topClause = top.HasValue ? $"TOP {top.Value} " : "";

      return $"""

              SELECT {topClause} 
              {Tevent.TeventType}, {Tevent.Tevent}, {Tevent.TaggregateId}, {Tevent.EffectiveVersion}, {Tevent.TeventId}, {Tevent.UtcTimeStamp}, {Tevent.InsertionOrder}, {Tevent.TargetTevent}, {Tevent.RefactoringType}, {Tevent.InsertedVersion}, cast({Tevent.ReadOrder} as varchar) as CharEffectiveOrder --The as is required, or Postgre sorts by this column when we ask it to sort by EffectiveOrder.
              FROM {Tevent.TableName}
              """;
   }

   // The TeventType column holds an interned int. We read it raw here and resolve it to the canonical type string
   // in ToDataRow AFTER the reader has closed — resolving during the read could open a second connection on a
   // cache miss while this reader is held.
   static (int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage) ReadRawDataRow(NpgsqlDataReader teventReader) => (
      TeventTypeId: teventReader.GetInt32(0),
      Json: teventReader.GetString(1),
      TeventId: new TessageId(teventReader.GetGuid(4)),
      Version: teventReader.GetInt32(3),
      TaggregateId: new TaggregateId(teventReader.GetGuid(2)),
      //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
      UtcTimeStamp: DateTime.SpecifyKind(teventReader.GetDateTime(5), DateTimeKind.Utc),
      Storage: new TaggregateTeventStorageInformation
               {
                  ReadOrder = ReadOrder.Parse(teventReader.GetString(10)),
                  InsertedVersion = teventReader.GetInt32(9),
                  EffectiveVersion = teventReader.GetInt32(3),
                  RefactoringInformation = (teventReader.IsDBNull(7) ? (Guid?)null : teventReader.GetGuid(7), teventReader[8] as short?)switch
                  {
                     (null, null) => null,
                     // ReSharper disable PatternAlwaysOfType
                     (Guid targetTevent, short type) => new TaggregateTeventRefactoringInformation(new TessageId(targetTevent), (TaggregateTeventRefactoringType)type),
                     // ReSharper restore PatternAlwaysOfType
                     (_, _) => throw new Exception("Should not be possible to get here")
                  }
               }
   );

   TeventDataRow ToDataRow((int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage) raw) =>
      new(_typeIdInterner.GetTypeId(raw.TeventTypeId), raw.Json, raw.TeventId, raw.Version, raw.TaggregateId, raw.UtcTimeStamp, raw.Storage);

   public IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
   {
      IReadOnlyList<TeventDataRow> GetHistory()
      {
         var raw = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                       command => command.SetCommandText($"""


                                                                          {CreateSelectClause()}
                                                                          WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}
                                                                              AND {Tevent.InsertedVersion} >= @CachedVersion
                                                                              AND {Tevent.EffectiveVersion} > 0
                                                                          ORDER BY {Tevent.ReadOrder} ASC;

                                                                          """)
                                                         .AddParameter(Tevent.TaggregateId, taggregateId.Value)
                                                         .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                         .PrepareStatement()
                                                         .ExecuteReaderAndSelect(ReadRawDataRow)
                                                         .SkipWhile(it => it.Storage.InsertedVersion <= startAfterInsertedVersion)
                                                         .ToList());
         return raw.Select(ToDataRow).ToList();
      }

      if(takeWriteLock)
      {
         _connectionManager.UseCommand(command => command.SetCommandText($"SELECT pg_advisory_xact_lock(hashtext(cast(@{Tevent.TaggregateId} as text)));")
                                                         .AddParameter(Tevent.TaggregateId, taggregateId.Value)
                                                         .PrepareStatement()
                                                         .ExecuteNonQuery());

         //Advisory lock serializes access. Since tevents are append-only, the lock is sufficient. Suppressing the current transaction keeps PostgreSql from incorrectly detecting a collision and failing our transactions.
         using var ignore = new TransactionScope(TransactionScopeOption.Suppress);
         return GetHistory();
      } else
      {
         return GetHistory();
      }
   }

   public IEnumerable<TeventDataRow> StreamTevents(int batchSize)
   {
      ReadOrder lastReadTeventReadOrder = default;
      int fetchedInThisBatch;
      do
      {
         var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                         command =>
                                                         {
                                                            var commandText = $"""

                                                                               {CreateSelectClause()} 
                                                                               WHERE {Tevent.ReadOrder}  > CAST(@{Tevent.ReadOrder} AS {Tevent.ReadOrderType})
                                                                                   AND {Tevent.EffectiveVersion} > 0
                                                                               ORDER BY {Tevent.ReadOrder} ASC
                                                                               LIMIT {batchSize}
                                                                               """;
                                                            return command.SetCommandText(commandText)
                                                                          .AddParameter(Tevent.ReadOrder, NpgsqlDbType.Varchar, lastReadTeventReadOrder.ToString())
                                                                          .PrepareStatement()
                                                                          .ExecuteReaderAndSelect(ReadRawDataRow)
                                                                          .ToList();
                                                         }).Select(ToDataRow).ToList();
         if(historyData.Any())
         {
            lastReadTeventReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value;
         }

         //We do not yield while reading from the reader since that may cause code to run that will cause another sql call into the same connection. Something that throws an exception unless you use an unusual and non-recommended connection string setting.
         foreach(var teventDataRow in historyData)
         {
            yield return teventDataRow;
         }

         fetchedInThisBatch = historyData.Count;
      } while(!(fetchedInThisBatch < batchSize));
   }

   public IReadOnlyList<CreationTeventRow> ListTaggregateIdsInCreationOrder()
   {
      var raw = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                           action: command => command.SetCommandText($"""

                                                                                      SELECT {Tevent.TaggregateId}, {Tevent.TeventType}
                                                                                      FROM {Tevent.TableName}
                                                                                      WHERE {Tevent.EffectiveVersion} = 1
                                                                                      ORDER BY {Tevent.ReadOrder} ASC
                                                                                      """)
                                                                     .PrepareStatement()
                                                                     .ExecuteReaderAndSelect(reader => (TaggregateId: new TaggregateId(reader.GetGuid(0)), TeventTypeId: reader.GetInt32(1))));
      return raw.Select(it => new CreationTeventRow(it.TaggregateId, _typeIdInterner.GetTypeId(it.TeventTypeId))).ToList();
   }
}