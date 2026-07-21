using Compze.Abstractions.Public;
using Compze.Internals.Sql.Common;
using Compze.Tessaging;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Microsoft.Data.Sqlite;
using Tevent = Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.TeventTableSchemaStrings;
using Compze.Internals.Sql.Sqlite.Internal;

namespace Compze.Teventive.TeventStore.Sqlite.Private;

partial class SqliteTeventStoreSqlLayer(SqliteTeventStoreConnectionManager connectionManager, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : ITeventStoreSqlLayer
{
   readonly SqliteTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   static string CreateSelectClause() => InternalSelect();

   static string InternalSelect(int? top = null)
   {
      var topClause = top.HasValue ? $"LIMIT {top.Value} " : "";

      return $"""

              SELECT 
              {Tevent.TeventType}, {Tevent.Tevent}, {Tevent.TaggregateId}, {Tevent.EffectiveVersion}, {Tevent.TeventId}, {Tevent.UtcTimeStamp}, {Tevent.InsertionOrder}, {Tevent.TargetTevent}, {Tevent.RefactoringType}, {Tevent.InsertedVersion}, {Tevent.ReadOrderIntegerPart}, {Tevent.ReadOrderFractionPart}
              FROM {Tevent.TableName}
              {topClause}
              """;
   }

   // The TeventType column holds an interned int. We read it raw here and resolve it to the canonical type string
   // in ToDataRow AFTER the reader has closed — resolving during the read could open a second connection on a
   // cache miss while this reader is held.
   static (int TeventTypeId, string Json, TessageId TeventId, int Version, TaggregateId TaggregateId, DateTime UtcTimeStamp, TaggregateTeventStorageInformation Storage) ReadRawDataRow(SqliteDataReader teventReader) => (
      TeventTypeId: teventReader.GetInt32(0),
      Json: teventReader.GetString(1),
      TeventId: new TessageId(teventReader.GetGuidFromString(4)),
      Version: teventReader.GetInt32(3),
      TaggregateId: new TaggregateId(teventReader.GetGuidFromString(2)),
      // DateTime stored as Ticks (INTEGER) for full precision
      UtcTimeStamp: new DateTime(teventReader.GetInt64(5), DateTimeKind.Utc),
      Storage: new TaggregateTeventStorageInformation
               {
                  ReadOrder = ReadOrder.FromParts(teventReader.GetInt64(10), teventReader.GetInt64(11)),
                  InsertedVersion = teventReader.GetInt32(9),
                  EffectiveVersion = teventReader.GetInt32(3),
                  RefactoringInformation = (teventReader.IsDBNull(7) ? (Guid?)null : teventReader.GetGuidFromString(7), teventReader.IsDBNull(8) ? (int?)null : teventReader.GetInt32(8))switch
                  {
                     (null, null)               => null,
                     ({} targetTevent, {} type) => new TaggregateTeventRefactoringInformation(new TessageId(targetTevent), (TaggregateTeventRefactoringType)type),
                     _                          => throw new Exception("Should not be possible to get here")
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
                                                                                 AND {Tevent.InsertedVersion} > @CachedVersion
                                                                                 AND {Tevent.EffectiveVersion} > 0
                                                                             ORDER BY {Tevent.ReadOrderIntegerPart} ASC, {Tevent.ReadOrderFractionPart} ASC
                                                                             """)
                                                            .AddMediumTextParameter(Tevent.TaggregateId, taggregateId.ToString())
                                                            .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                            .ExecuteReaderAndSelect(ReadRawDataRow)
                                                            .ToList());
      return raw.Select(ToDataRow).ToList();
   }

   public IEnumerable<TeventDataRow> StreamTevents(int batchSize)
   {
      var lastReadOrder = ReadOrder.Zero;
      int fetchedInThisBatch;
      do
      {
         var raw = _connectionManager.UseCommand(suppressTransactionWarning: true,
                                                         command => command.SetCommandText($"""

                                                                                            {CreateSelectClause()}
                                                                                            WHERE ({Tevent.ReadOrderIntegerPart} > @LastIntegerPart
                                                                                                OR ({Tevent.ReadOrderIntegerPart} = @LastIntegerPart AND {Tevent.ReadOrderFractionPart} > @LastFractionPart))
                                                                                                AND {Tevent.EffectiveVersion} > 0
                                                                                            ORDER BY {Tevent.ReadOrderIntegerPart} ASC, {Tevent.ReadOrderFractionPart} ASC
                                                                                            LIMIT {batchSize}
                                                                                            """)
                                                                           .AddParameter("LastIntegerPart", lastReadOrder.IntegerPart)
                                                                           .AddParameter("LastFractionPart", lastReadOrder.FractionPart)
                                                                           .ExecuteReaderAndSelect(ReadRawDataRow)
                                                                           .ToList());
         var historyData = raw.Select(ToDataRow).ToList();
         if(historyData.Any())
         {
            lastReadOrder = historyData[^1].StorageInformation.ReadOrder!.Value;
         }

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
                                                                                      ORDER BY {Tevent.ReadOrderIntegerPart} ASC, {Tevent.ReadOrderFractionPart} ASC
                                                                                      """)
                                                                     .ExecuteReaderAndSelect(reader => (TaggregateId: new TaggregateId(reader.GetGuidFromString(0)), TeventTypeId: reader.GetInt32(1))));
      return raw.Select(it => new CreationTeventRow(it.TaggregateId, _typeIdInterner.GetTypeId(it.TeventTypeId))).ToList();
   }
}

