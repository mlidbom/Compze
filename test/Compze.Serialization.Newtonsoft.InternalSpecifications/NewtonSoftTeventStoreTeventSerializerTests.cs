using Compze.Abstractions;

using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Compze.Teventive.Taggregates.Tevents._internal;

namespace Compze.Serialization.Newtonsoft.InternalSpecifications;

public class NewtonSoftTeventStoreTeventSerializerTests : SerializerTest
{
   [PCTSerializer]
   public void IgnoresAllITaggregateTeventProperties()
   {
      var teventWithAllValuesSet = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         taggregateId: TaggregateId.New(),
         taggregateVersion: 2,
         utcTimeStamp: DateTime.UtcNow + 1.Minutes());

      var teventWithOnlySubclassValues = new TestTevent("Test1", "Test2");
      ((IMutableTaggregateTevent)teventWithOnlySubclassValues).SetUtcTimeStampInternal(DateTime.MinValue);

      var teventWithAllValuesJson = TeventSerializer.Serialize(new TaggregateTevent<TestTevent>(teventWithAllValuesSet));
      var teventWithOnlySubclassValuesJson = TeventSerializer.Serialize(new TaggregateTevent<TestTevent>(teventWithOnlySubclassValues));
      var roundTripped = (TestTevent)TeventSerializer.Deserialize(typeof(TaggregateTevent<TestTevent>), teventWithAllValuesJson).Tevent;

      teventWithAllValuesJson.Must().Be("""
                                        {
                                          "Tevent": {
                                            "Test1": "Test1",
                                            "Test2": "Test2"
                                          }
                                        }
                                        """);
      teventWithAllValuesJson.Must().Be(teventWithOnlySubclassValuesJson);

      roundTripped.Must(serialization: it => it.ExcludeMember(tevent => tevent.UtcTimeStamp) //Timestamp is defaulted in the constructor used by serialization.
                                               .ExcludeMember(tevent => tevent.Id))
                  .DeepEqual(teventWithOnlySubclassValues);
   }

   public class TestTevent : TaggregateTeventWithIdentity
   {
      [JsonConstructor] public TestTevent(string test1, string test2)
      {
         Test1 = test1;
         Test2 = test2;
      }

      public TestTevent(
         string test1,
         string test2,
         int taggregateVersion,
         TaggregateId taggregateId,
         DateTime utcTimeStamp) : base(taggregateId)
      {
         Test1 = test1;
         Test2 = test2;

         ((IMutableTaggregateTevent)this).SetTaggregateVersionInternal(taggregateVersion);
         ((IMutableTaggregateTevent)this).SetUtcTimeStampInternal(utcTimeStamp);
      }

      // ReSharper disable once MemberCanBePrivate.Local
      public string Test1 { [UsedImplicitly] get; private set; }

      // ReSharper disable once MemberCanBePrivate.Local
      public string Test2 { [UsedImplicitly] get; private set; }
   }
}
