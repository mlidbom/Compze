using Compze.Abstractions.Public;
using Compze.Internals.Sql.Common;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using MySqlConnector;
using Tevent = Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.TeventTableSchemaStrings;
using Compze.Internals.Sql.MySql._internal;

namespace Compze.Teventive.TeventStore.MySql._private;

partial class MySqlTeventStoreSqlLayer(MySqlTeventStoreConnectionManager connectionManager, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : ITeventStoreSqlLayer
{
   readonly MySqlTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   static string CreateSelectClause() => InternalSelect();

   static string CreateLockHint(bool takeWriteLock) => takeWriteLock ? "FOR UPDATE" : "";
   // ReSharper disable once UnusedParameter.Local
   static string InternalSelect(int? top = null)
   {
      var topClause = top.HasValue ? $"TOP {top.Value} " : "";
      //var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";
      const string lockHint = "";

      return $"""

              SELECT {topClause} 
              {Tevent.TeventType}, {Tevent.Tevent}, {Tevent.TaggregateId}, {Tevent.EffectiveVersion}, {Tevent.TeventId}, {Tevent.UtcTimeStamp}, {Tevent.InsertionOrder}, {Tevent.TargetTevent}, {Tevent.RefactoringType}, {Tevent.InsertedVersion}, cast({Tevent.ReadOrder} as char(39))
              FROM {Tevent.TableName} {lockHint} 
              """;
   }

   // The TeventType column holds an interned int. We read it raw here and resolve it to the canonical type string
   // in ToDataRow AFTER the reader has closed — resolving during the read could open a second connection on a
   // cache miss while this reader is held.
   static (int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage) ReadRawDataRow(MySqlDataReader teventReader) => (
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
                  RefactoringInformation = (teventReader[7] as Guid?, teventReader[8] as sbyte?)switch
                  {
                     (null, null) => null,
                     // ReSharper disable PatternAlwaysOfType
                     (Guid targetTevent, sbyte type) => new TaggregateTeventRefactoringInformation(new TessageId(targetTevent), (TaggregateTeventRefactoringType)type),
                     // ReSharper restore PatternAlwaysOfType
                     _ => throw new Exception("Should not be possible to get here")
                  }
               }
   );

   TeventDataRow ToDataRow((int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage) raw) =>
      new(_typeIdInterner.GetTypeId(raw.TeventTypeId), raw.Json, raw.TeventId, raw.Version, raw.TaggregateId, raw.UtcTimeStamp, raw.Storage);

   public IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
   {
      var raw = _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                    command => command.SetCommandText($"""

                                                                       {CreateSelectClause()}
                                                                       WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}
                                                                           AND {Tevent.InsertedVersion} >= @CachedVersion
                                                                           AND {Tevent.EffectiveVersion} > 0
                                                                       ORDER BY {Tevent.ReadOrder} ASC
                                                                       {CreateLockHint(takeWriteLock)}
                                                                       """)
                                                      .AddParameter(Tevent.TaggregateId, taggregateId.Value)
                                                      .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                      .ExecuteReaderAndSelect(ReadRawDataRow)
                                                      .SkipWhile(row => row.Storage.InsertedVersion <= startAfterInsertedVersion)
                                                      .ToList());
      return raw.Select(ToDataRow).ToList();
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
                                                                          .AddParameter(Tevent.ReadOrder, MySqlDbType.String, lastReadTeventReadOrder.ToString())
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
                                                                     .ExecuteReaderAndSelect(reader => (TaggregateId: new TaggregateId(reader.GetGuid(0)), TeventTypeId: reader.GetInt32(1))));
      return raw.Select(it => new CreationTeventRow(it.TaggregateId, _typeIdInterner.GetTypeId(it.TeventTypeId))).ToList();
   }
}