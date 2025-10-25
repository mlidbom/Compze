using System;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public class TeventDataRow
{
   public TeventDataRow(AggregateTeventData @tevent, AggregateTeventStorageInformation storageInformation, Guid teventType, string teventAsJson)
   {
      TeventJson = teventAsJson;
      TeventType = teventType;

      TeventId = @tevent.TessageId;
      AggregateVersion = @tevent.AggregateVersion;
      AggregateId = @tevent.AggregateId;
      UtcTimeStamp = @tevent.UtcTimeStamp;

      StorageInformation = storageInformation;
   }

   public TeventDataRow(TeventInsertionSpecification specification, Guid typeId, string teventAsJson)
   {
      var @tevent = specification.Tevent;
      TeventJson = teventAsJson;
      TeventType = typeId;

      TeventId = @tevent.TessageId;
      AggregateVersion = @tevent.AggregateVersion;
      AggregateId = @tevent.AggregateId;
      UtcTimeStamp = @tevent.UtcTimeStamp;

      StorageInformation = new AggregateTeventStorageInformation
                           {
                              InsertedVersion = specification.InsertedVersion,
                              EffectiveVersion = specification.EffectiveVersion
                           };
   }

   public TeventDataRow(Guid teventType, string teventJson, Guid teventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, AggregateTeventStorageInformation storageInformation)
   {
      TeventType = teventType;
      TeventJson = teventJson;
      TeventId = teventId;
      AggregateVersion = aggregateVersion;
      AggregateId = aggregateId;
      UtcTimeStamp = utcTimeStamp;

      StorageInformation = storageInformation;
   }

   public Guid TeventType { get; private set; }
   public string TeventJson { get; private set; }
   public Guid TeventId { get; private set; }
   public int AggregateVersion { get; private set; }

   public Guid AggregateId { get; private set; }
   public DateTime UtcTimeStamp { get; private set; }

   public AggregateTeventStorageInformation StorageInformation { get; private set; }

   public override string ToString() => $"{nameof(StorageInformation.InsertedVersion)}{StorageInformation.InsertedVersion},{nameof(StorageInformation.EffectiveVersion)}{StorageInformation.EffectiveVersion}, {nameof(StorageInformation.ReadOrder)}{StorageInformation.ReadOrder}";
}