using System.Transactions;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Common._internal;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Npgsql;
using NpgsqlTypes;
using Tevent = Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.TeventTableSchemaStrings;
using Compze.Sql.PostgreSql._internal;

namespace Compze.Teventive.TeventStore.PostgreSql._private;

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

   ///<summary>Reads the history of <paramref name="taggregateId"/>, after taking its lock when <paramref name="takeWriteLock"/>: in the<br/>
   /// transaction, on its connection.</summary>
   public IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
   {
      if(takeWriteLock) TakeTheTaggregatesLock(taggregateId);

      var raw = _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                              command => TheHistoryCommand(command, taggregateId, startAfterInsertedVersion).PrepareStatement().ExecuteReaderAndSelect(ReadRawDataRow));
      return TheRowsAfter(startAfterInsertedVersion, raw);
   }

   ///<summary>As <see cref="GetTaggregateHistory"/> taking the lock, waiting for the lock holding no thread.</summary>
   public async Task<IReadOnlyList<TeventDataRow>> GetTaggregateHistoryForUpdateAsync(TaggregateId taggregateId, int startAfterInsertedVersion)
   {
      await TakeTheTaggregatesLockAsync(taggregateId).caf();

      var raw = await _connectionManager.UseCommandAsync(suppressTransactionWarning: false,
                                                         command => TheHistoryCommand(command, taggregateId, startAfterInsertedVersion).ExecutePreparedReaderAndSelectAsync(ReadRawDataRow)).caf();
      return TheRowsAfter(startAfterInsertedVersion, raw);
   }

   ///<summary>Takes the taggregate's lock for the transaction: an advisory lock on its id, which PostgreSQL holds until the transaction<br/>
   /// commits or rolls back, and which a transaction that asks for it meanwhile waits for.</summary>
   ///<remarks>The read of the history after it, in the same transaction, sees every tevent committed before the read began, those of the<br/>
   /// lock's previous holder included: at <see cref="IsolationLevel.ReadCommitted"/>, the isolation level of a unit of work's transaction,<br/>
   /// each statement reads what was committed when it began. See <see cref="AssertTheTransactionIsAtReadCommitted"/> for higher levels.</remarks>
   void TakeTheTaggregatesLock(TaggregateId taggregateId)
   {
      AssertTheTransactionIsAtReadCommitted();
      _connectionManager.UseCommand(command => TheTaggregatesLockCommand(command, taggregateId).PrepareStatement().ExecuteNonQuery());
   }

   ///<summary>As <see cref="TakeTheTaggregatesLock"/>, waiting for the lock holding no thread.</summary>
   async Task TakeTheTaggregatesLockAsync(TaggregateId taggregateId)
   {
      AssertTheTransactionIsAtReadCommitted();
      await _connectionManager.UseCommandAsync(suppressTransactionWarning: false, command => TheTaggregatesLockCommand(command, taggregateId).ExecutePreparedNonQueryAsync()).caf();
   }

   ///<summary>Asserts that the ambient transaction, if there is one, is at <see cref="IsolationLevel.ReadCommitted"/>, as a unit of work's is.</summary>
   ///<remarks>At a higher isolation level, a transaction reads the database as it was when its first statement began, which may be before<br/>
   /// the lock's previous holder committed. Its read after the lock would then miss the tevents that holder added. Until 2026-03-16, units<br/>
   /// of work ran at <see cref="IsolationLevel.Serializable"/>, and their reads for update failed with serialization failures; this store<br/>
   /// read the history outside the transaction, on a second connection, for that reason.</remarks>
   static void AssertTheTransactionIsAtReadCommitted() =>
      Contract.State.Assert(Transaction.Current is not {} transaction || transaction.IsolationLevel == IsolationLevel.ReadCommitted,
                            () => $"The PostgreSQL tevent store gets a taggregate for update only in a transaction at {IsolationLevel.ReadCommitted}, as a unit of work's is, and this one is at {Transaction.Current!.IsolationLevel}. "
                                + "At a higher isolation level, a transaction reads the database as it was when its first statement began, which may be before the unit of work that held the taggregate's lock committed: its read would miss the tevents that unit of work added.");

   static NpgsqlCommand TheTaggregatesLockCommand(NpgsqlCommand command, TaggregateId taggregateId) =>
      command.SetCommandText($"SELECT pg_advisory_xact_lock(hashtext(cast(@{Tevent.TaggregateId} as text)));")
             .AddParameter(Tevent.TaggregateId, taggregateId.Value);

   static NpgsqlCommand TheHistoryCommand(NpgsqlCommand command, TaggregateId taggregateId, int startAfterInsertedVersion) =>
      command.SetCommandText($"""


                             {CreateSelectClause()}
                             WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}
                                 AND {Tevent.InsertedVersion} >= @CachedVersion
                                 AND {Tevent.EffectiveVersion} > 0
                             ORDER BY {Tevent.ReadOrder} ASC;

                             """)
             .AddParameter(Tevent.TaggregateId, taggregateId.Value)
             .AddParameter("CachedVersion", startAfterInsertedVersion);

   IReadOnlyList<TeventDataRow> TheRowsAfter(int startAfterInsertedVersion, IEnumerable<(int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage)> raw) =>
      [..raw.SkipWhile(it => it.Storage.InsertedVersion <= startAfterInsertedVersion).Select(ToDataRow)];

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

   public IReadOnlyList<TaggregateTeventTypeRow> ListTaggregateIdsInCreationOrder()
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
                                                                     .PrepareStatement()
                                                                     .ExecuteReaderAndSelect(reader => (TaggregateId: new TaggregateId(reader.GetGuid(0)), TeventTypeId: reader.GetInt32(1))));
      return [..raw.Select(it => new TaggregateTeventTypeRow(it.TaggregateId, _typeIdInterner.GetTypeId(it.TeventTypeId)))];
   }
}