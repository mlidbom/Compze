using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.Common;
using Microsoft.Data.Sqlite;
using Tevent = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Sql.Sqlite.Private.TEventStore;

partial class SqliteTeventStoreSqlLayer(SqliteTeventStoreConnectionManager connectionManager, SqliteSqlLayerSchemaManager schemaManager) : ITeventStoreSqlLayer
{
   readonly SqliteTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

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

   static TeventDataRow ReadDataRow(SqliteDataReader teventReader) => new(
      teventType: new TypeId(Guid.Parse(teventReader.GetString(0))),
      teventJson: teventReader.GetString(1),
      teventId: new TessageId(Guid.Parse(teventReader.GetString(4))),
      taggregateVersion: teventReader.GetInt32(3),
      taggregateId: new TaggregateId(Guid.Parse(teventReader.GetString(2))),
      // DateTime stored as Ticks (INTEGER) for full precision
      utcTimeStamp: new DateTime(teventReader.GetInt64(5), DateTimeKind.Utc),
      storageInformation: new TaggregateTeventStorageInformation
                          {
                             ReadOrder = ReadOrder.FromParts(teventReader.GetInt64(10), teventReader.GetInt64(11)),
                             InsertedVersion = teventReader.GetInt32(9),
                             EffectiveVersion = teventReader.GetInt32(3),
                             RefactoringInformation = (teventReader.IsDBNull(7) ? (Guid?)null : Guid.Parse(teventReader.GetString(7)), teventReader.IsDBNull(8) ? (int?)null : teventReader.GetInt32(8))switch
                             {
                                (null, null)               => null,
                                ({} targetTevent, {} type) => new TaggregateTeventRefactoringInformation(new TessageId(targetTevent), (TaggregateTeventRefactoringType)type),
                                _                          => throw new Exception("Should not be possible to get here")
                             }
                          }
   );

   public IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
   {

      return _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
                                          command => command.SetCommandText($"""

                                                                             {CreateSelectClause()} 
                                                                             WHERE {Tevent.TaggregateId} = @{Tevent.TaggregateId}
                                                                                 AND {Tevent.InsertedVersion} > @CachedVersion
                                                                                 AND {Tevent.EffectiveVersion} > 0
                                                                             ORDER BY {Tevent.ReadOrderIntegerPart} ASC, {Tevent.ReadOrderFractionPart} ASC
                                                                             """)
                                                            .AddVarcharParameter(Tevent.TaggregateId, 36, taggregateId.ToString())
                                                            .AddParameter("CachedVersion", startAfterInsertedVersion)
                                                            .ExecuteReaderAndSelect(ReadDataRow)
                                                            .ToList());
   }

   public IEnumerable<TeventDataRow> StreamTevents(int batchSize)
   {
      var lastReadOrder = ReadOrder.Zero;
      int fetchedInThisBatch;
      do
      {
         var historyData = _connectionManager.UseCommand(suppressTransactionWarning: true,
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
                                                                           .ExecuteReaderAndSelect(ReadDataRow)
                                                                           .ToList());
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
      return _connectionManager.UseCommand(suppressTransactionWarning: true,
                                           action: command => command.SetCommandText($"""

                                                                                      SELECT {Tevent.TaggregateId}, {Tevent.TeventType} 
                                                                                      FROM {Tevent.TableName} 
                                                                                      WHERE {Tevent.EffectiveVersion} = 1 
                                                                                      ORDER BY {Tevent.ReadOrderIntegerPart} ASC, {Tevent.ReadOrderFractionPart} ASC
                                                                                      """)
                                                                     .ExecuteReaderAndSelect(reader => new CreationTeventRow(taggregateId: new TaggregateId(Guid.Parse(reader.GetString(0))), typeId: Guid.Parse(reader.GetString(1)))));
   }
}

