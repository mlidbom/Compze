using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class TeventDataRow
{
   public TeventDataRow(TaggregateTeventData tevent, TaggregateTeventStorageInformation storageInformation, MappedTypeId teventType, string teventAsJson)
   {
      TeventJson = teventAsJson;
      TeventType = teventType;

      TeventId = tevent.TessageId;
      TaggregateVersion = tevent.TaggregateVersion;
      TaggregateId = tevent.TaggregateId;
      UtcTimeStamp = tevent.UtcTimeStamp;

      StorageInformation = storageInformation;
   }

   public TeventDataRow(TeventInsertionSpecification specification, MappedTypeId typeId, string teventAsJson)
   {
      var tevent = specification.Tevent;
      TeventJson = teventAsJson;
      TeventType = typeId;

      TeventId = tevent.TessageId;
      TaggregateVersion = tevent.TaggregateVersion;
      TaggregateId = tevent.TaggregateId;
      UtcTimeStamp = tevent.UtcTimeStamp;

      StorageInformation = new TaggregateTeventStorageInformation
                           {
                              InsertedVersion = specification.InsertedVersion,
                              EffectiveVersion = specification.EffectiveVersion
                           };
   }

   public TeventDataRow(MappedTypeId teventType, string teventJson, TessageId teventId, int taggregateVersion, TaggregateId taggregateId, DateTime utcTimeStamp, TaggregateTeventStorageInformation storageInformation)
   {
      TeventType = teventType;
      TeventJson = teventJson;
      TeventId = teventId;
      TaggregateVersion = taggregateVersion;
      TaggregateId = taggregateId;
      UtcTimeStamp = utcTimeStamp;

      StorageInformation = storageInformation;
   }

   public MappedTypeId TeventType { get; private set; }
   public string TeventJson { get; private set; }
   public TessageId TeventId { get; private set; }
   public int TaggregateVersion { get; private set; }

   public TaggregateId TaggregateId { get; private set; }
   public DateTime UtcTimeStamp { get; private set; }

   public TaggregateTeventStorageInformation StorageInformation { get; private set; }

   public override string ToString() => $"{nameof(StorageInformation.InsertedVersion)}{StorageInformation.InsertedVersion},{nameof(StorageInformation.EffectiveVersion)}{StorageInformation.EffectiveVersion}, {nameof(StorageInformation.ReadOrder)}{StorageInformation.ReadOrder}";
}