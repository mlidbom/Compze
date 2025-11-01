using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.Common;
using MySql.Data.MySqlClient;
using Tevent = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.TeventTableSchemaStrings;

namespace Compze.Sql.MySql.Private.TEventStore;

internal partial class MySqlTeventStoreSqlLayer(MySqlTeventStoreConnectionManager connectionManager, MySqlSqlLayerSchemaManager schemaManager) : ITeventStoreSqlLayer
{
   readonly MySqlTeventStoreConnectionManager _connectionManager = connectionManager;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;

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

   static TeventDataRow ReadDataRow(MySqlDataReader teventReader)
   {
      return new TeventDataRow(
         teventType: new TypeId(teventReader.GetGuid(0)),
         teventJson: teventReader.GetString(1),
         teventId: new TessageId(teventReader.GetGuid(4)),
         taggregateVersion: teventReader.GetInt32(3),
         taggregateId: new TaggregateId(teventReader.GetGuid(2)),
         //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
         utcTimeStamp: DateTime.SpecifyKind(teventReader.GetDateTime(5), DateTimeKind.Utc),
         storageInformation: new TaggregateTeventStorageInformation
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
   }

   public IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0) =>
      _connectionManager.UseCommand(suppressTransactionWarning: !takeWriteLock,
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
                                                      .ExecuteReaderAndSelect(ReadDataRow)
                                                      .SkipWhile(row => row.StorageInformation.InsertedVersion <= startAfterInsertedVersion)
                                                      .ToList());

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
                                                                          .ExecuteReaderAndSelect(ReadDataRow)
                                                                          .ToList();
                                                         });
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
      return _connectionManager.UseCommand(suppressTransactionWarning: true,
                                           action: command => command.SetCommandText($"""

                                                                                      SELECT {Tevent.TaggregateId}, {Tevent.TeventType} 
                                                                                      FROM {Tevent.TableName} 
                                                                                      WHERE {Tevent.EffectiveVersion} = 1 
                                                                                      ORDER BY {Tevent.ReadOrder} ASC
                                                                                      """)
                                                                     .ExecuteReaderAndSelect(reader => new CreationTeventRow(taggregateId: new TaggregateId(reader.GetGuid(0)), typeId: reader.GetGuid(1))));
   }
}