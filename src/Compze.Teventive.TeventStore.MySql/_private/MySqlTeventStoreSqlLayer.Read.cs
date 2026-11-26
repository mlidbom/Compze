using Compze.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._internal;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using MySqlConnector;
using Tevent = Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.TeventTableSchemaStrings;
using Compze.Sql.MySql._internal;

namespace Compze.Teventive.TeventStore.MySql._private;

partial class MySqlTeventStoreSqlLayer(MySqlTeventStoreConnectionManager connectionManager, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : ITeventStoreSqlLayer
{
   readonly MySqlTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   static string CreateSelectClause() => InternalSelect();

   ///<summary>A read for update reads with FOR UPDATE, a locking read, which sees the latest committed tevents whatever snapshot the<br/>
   /// transaction's earlier reads have at REPEATABLE READ. The lock that serializes the units of work is the taggregate's row in the lock<br/>
   /// table, <see cref="TakeTheTaggregatesLock"/>: FOR UPDATE locks no row of a taggregate with no history.</summary>
   static string CreateLockHint(bool takeWriteLock) => takeWriteLock ? "FOR UPDATE" : "";

   ///<summary>Takes the taggregate's lock for the transaction: its row in the lock table, inserted if it has none. InnoDB holds the row's<br/>
   /// lock until the transaction commits or rolls back, at any isolation level, and a transaction that asks for it meanwhile waits. So<br/>
   /// a unit of work that finds no taggregate to get for update keeps another from adding it until it completes, as SQL Server's<br/>
   /// application lock and PostgreSQL's advisory lock on the id do. The table keeps a row for every taggregate ever got for update.</summary>
   void TakeTheTaggregatesLock(TaggregateId taggregateId) =>
      _connectionManager.UseCommand(command => TheTaggregatesLockCommand(command, taggregateId).ExecuteNonQuery());

   ///<summary>As <see cref="TakeTheTaggregatesLock"/>, waiting for the lock holding no thread.</summary>
   async Task TakeTheTaggregatesLockAsync(TaggregateId taggregateId) =>
      await _connectionManager.UseCommandAsync(suppressTransactionWarning: false, command => TheTaggregatesLockCommand(command, taggregateId).ExecuteNonQueryAsync()).caf();

   static MySqlCommand TheTaggregatesLockCommand(MySqlCommand command, TaggregateId taggregateId) =>
      command.SetCommandText($"INSERT INTO {TaggregateLockTableName} ({Tevent.TaggregateId}) VALUES (@{Tevent.TaggregateId}) ON DUPLICATE KEY UPDATE {Tevent.TaggregateId} = {Tevent.TaggregateId};")
             .AddParameter(Tevent.TaggregateId, taggregateId.Value);
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
      if(takeWriteLock) TakeTheTaggregatesLock(taggregateId);

      var raw = _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                              command => TheHistoryCommand(command, taggregateId, takeWriteLock, startAfterInsertedVersion).ExecuteReaderAndSelect(ReadRawDataRow));
      return TheRowsAfter(startAfterInsertedVersion, raw);
   }

   public async Task<IReadOnlyList<TeventDataRow>> GetTaggregateHistoryForUpdateAsync(TaggregateId taggregateId, int startAfterInsertedVersion)
   {
      await TakeTheTaggregatesLockAsync(taggregateId).caf();

      var raw = await _connectionManager.UseCommandAsync(suppressTransactionWarning: false,
                                                         command => TheHistoryCommand(command, taggregateId, takeWriteLock: true, startAfterInsertedVersion).ExecuteReaderAndSelectAsync(ReadRawDataRow)).caf();
      return TheRowsAfter(startAfterInsertedVersion, raw);
   }

   static MySqlCommand TheHistoryCommand(MySqlCommand command, TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion) =>
      command.SetCommandText($"""

                             {CreateSelectClause()}
                             WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}
                                 AND {Tevent.InsertedVersion} >= @CachedVersion
                                 AND {Tevent.EffectiveVersion} > 0
                             ORDER BY {Tevent.ReadOrder} ASC
                             {CreateLockHint(takeWriteLock)}
                             """)
             .AddParameter(Tevent.TaggregateId, taggregateId.Value)
             .AddParameter("CachedVersion", startAfterInsertedVersion);

   IReadOnlyList<TeventDataRow> TheRowsAfter(int startAfterInsertedVersion, IEnumerable<(int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage)> raw) =>
      [..raw.SkipWhile(row => row.Storage.InsertedVersion <= startAfterInsertedVersion).Select(ToDataRow)];

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

   public IReadOnlyList<TaggregateTeventTypeRow> ListTaggregateIdsInCreationOrder()
   {
      var raw = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                           action: command => command.SetCommandText($"""

                                                                                      SELECT {Tevent.TaggregateId}, {Tevent.TeventType}
                                                                                      FROM {Tevent.TableName}
                                                                                      WHERE {Tevent.EffectiveVersion} = 1
                                                                                      ORDER BY {Tevent.ReadOrder} ASC
                                                                                      """)
                                                                     .ExecuteReaderAndSelect(reader => (TaggregateId: new TaggregateId(reader.GetGuid(0)), TeventTypeId: reader.GetInt32(1))));
      return [..raw.Select(it => new TaggregateTeventTypeRow(it.TaggregateId, _typeIdInterner.GetTypeId(it.TeventTypeId)))];
   }

   public IReadOnlyList<TaggregateTeventTypeRow> ListTheLastTeventOfEachTaggregate()
   {
      var raw = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                           action: command => command.SetCommandText($"""

                                                                                      SELECT lastRow.{Tevent.TaggregateId}, lastRow.{Tevent.TeventType}
                                                                                      FROM {Tevent.TableName} lastRow
                                                                                      INNER JOIN (SELECT {Tevent.TaggregateId}, MAX({Tevent.EffectiveVersion}) AS LastVersion
                                                                                                  FROM {Tevent.TableName}
                                                                                                  GROUP BY {Tevent.TaggregateId}) lastVersions
                                                                                              ON lastVersions.{Tevent.TaggregateId} = lastRow.{Tevent.TaggregateId}
                                                                                             AND lastVersions.LastVersion = lastRow.{Tevent.EffectiveVersion}
                                                                                      """)
                                                                     .ExecuteReaderAndSelect(reader => (TaggregateId: new TaggregateId(reader.GetGuid(0)), TeventTypeId: reader.GetInt32(1))));
      return [..raw.Select(it => new TaggregateTeventTypeRow(it.TaggregateId, _typeIdInterner.GetTypeId(it.TeventTypeId)))];
   }
}