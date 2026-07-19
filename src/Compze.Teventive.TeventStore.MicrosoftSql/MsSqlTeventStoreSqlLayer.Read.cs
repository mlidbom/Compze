using System.Data;
using System.Data.SqlTypes;
using Compze.Abstractions.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Tessaging.Abstractions.Public;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Microsoft.Data.SqlClient;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Teventive.TeventStore.MicrosoftSql;

partial class MsSqlTeventStoreSqlLayer(MsSqlTeventStoreConnectionManager connectionManager, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : ITeventStoreSqlLayer
{
   readonly MsSqlTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   static string CreateSelectClause(bool takeWriteLock) => InternalSelect(takeWriteLock: takeWriteLock);
   static string CreateSelectTopClause(int top, bool takeWriteLock) => InternalSelect(top: top, takeWriteLock: takeWriteLock);

   static string InternalSelect(bool takeWriteLock, int? top = null)
   {
      var topClause = top.HasValue ? $"TOP {top.Value} " : "";
      var lockHint = "With(READCOMMITTED, ROWLOCK)";
      var appLockPrefix = takeWriteLock
         ? $"""
            DECLARE @LockResource nvarchar(36) = cast(@{Tevent.TaggregateId} as nvarchar(36));
            EXEC sp_getapplock @Resource = @LockResource, @LockMode = 'Exclusive';

            """
         : "";

      return $"""
              {appLockPrefix}SELECT {topClause} 
              {Tevent.TeventType}, {Tevent.Tevent}, {Tevent.TaggregateId}, {Tevent.EffectiveVersion}, {Tevent.TeventId}, {Tevent.UtcTimeStamp}, {Tevent.InsertionOrder}, {Tevent.TargetTevent}, {Tevent.RefactoringType}, {Tevent.InsertedVersion}, {Tevent.ReadOrder}
              FROM {Tevent.TableName} {lockHint} 
              """;
   }

   // The TeventType column holds an interned int. We read it raw here and resolve it to the canonical type string
   // in ToDataRow AFTER the reader has closed — resolving during the read could open a second connection on a
   // cache miss while this reader is held.
   static (int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage) ReadRawDataRow(SqlDataReader teventReader) => (
      TeventTypeId: teventReader.GetInt32(0),
      Json: teventReader.GetString(1),
      TeventId: new TessageId(teventReader.GetGuid(4)),
      Version: teventReader.GetInt32(3),
      TaggregateId: new TaggregateId(teventReader.GetGuid(2)),
      //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
      UtcTimeStamp: DateTime.SpecifyKind(teventReader.GetDateTime(5), DateTimeKind.Utc),
      Storage: new TaggregateTeventStorageInformation
               {
                  ReadOrder = ReadOrder.FromSqlDecimal(teventReader.GetSqlDecimal(10)),
                  InsertedVersion = teventReader.GetInt32(9),
                  EffectiveVersion = teventReader.GetInt32(3),
                  RefactoringInformation = (teventReader[7] as Guid?, teventReader[8] as byte?)switch
                  {
                     (null, null) => null,
                     // ReSharper disable PatternAlwaysOfType
                     (Guid targetTevent, byte type) => new TaggregateTeventRefactoringInformation(new TessageId(targetTevent), (TaggregateTeventRefactoringType)type),
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

                                                                       {CreateSelectClause(takeWriteLock)}
                                                                       WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}
                                                                           AND {Tevent.InsertedVersion} > @CachedVersion
                                                                           AND {Tevent.EffectiveVersion} > 0
                                                                       ORDER BY {Tevent.ReadOrder} ASC
                                                                       """)
                                                      .AddParameter(Tevent.TaggregateId, taggregateId.Value)
                                                      .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                      .ExecuteReaderAndSelect(ReadRawDataRow)
                                                      .ToList());
      return raw.Select(ToDataRow).ToList();
   }

   public IEnumerable<TeventDataRow> StreamTevents(int batchSize)
   {
      SqlDecimal lastReadTeventReadOrder = 0;
      int fetchedInThisBatch;
      do
      {
         var raw = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                         command => command.SetCommandText($"""

                                                                                            {CreateSelectTopClause(batchSize, takeWriteLock: false)}
                                                                                            WHERE {Tevent.ReadOrder}  > @{Tevent.ReadOrder}
                                                                                                AND {Tevent.EffectiveVersion} > 0
                                                                                            ORDER BY {Tevent.ReadOrder} ASC
                                                                                            """)
                                                                           .AddParameter(Tevent.ReadOrder, SqlDbType.Decimal, lastReadTeventReadOrder)
                                                                           .ExecuteReaderAndSelect(ReadRawDataRow)
                                                                           .ToList());
         var historyData = raw.Select(ToDataRow).ToList();
         if(historyData.Any())
         {
            lastReadTeventReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value.ToSqlDecimal();
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
