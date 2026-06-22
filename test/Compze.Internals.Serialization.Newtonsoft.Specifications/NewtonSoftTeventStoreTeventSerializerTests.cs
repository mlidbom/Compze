using Compze.Abstractions.Public;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Internals.Serialization.Newtonsoft.Specifications;

public class NewtonSoftTeventStoreTeventSerializerTests : SerializerTest
{
   [PCTSerializer]
   public void IgnoresAllITaggregateTeventProperties()
   {
      var teventWithAllValuesSet = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         taggregateId: new TaggregateId(),
         taggregateVersion: 2,
         utcTimeStamp: DateTime.UtcNow + 1.Minutes());

      var teventWithOnlySubclassValues = new TestTevent("Test1", "Test2");
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)teventWithOnlySubclassValues).SetUtcTimeStampInternal(DateTime.MinValue);
#pragma warning restore CS0618 // Type or member is obsolete

      var teventWithAllValuesJson = TeventSerializer.Serialize(teventWithAllValuesSet);
      var teventWithOnlySubclassValuesJson = TeventSerializer.Serialize(teventWithOnlySubclassValues);
      var roundTripped = (TestTevent)TeventSerializer.Deserialize(typeof(TestTevent), teventWithAllValuesJson);

      teventWithAllValuesJson.Must().Be("""
                                        {
                                          "Test1": "Test1",
                                          "Test2": "Test2"
                                        }
                                        """);
      teventWithAllValuesJson.Must().Be(teventWithOnlySubclassValuesJson);

      roundTripped.Must().DeepEqual(teventWithOnlySubclassValues,
                                    config => config
                                             .ExcludeMember(tevent => tevent.UtcTimeStamp) //Timestamp is defaulted in the constructor used by serialization.
                                             .ExcludeMember(tevent => tevent.Id));
   }

   public class TestTevent : TaggregateTevent
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

#pragma warning disable CS0618 // Type or member is obsolete
         ((IMutableTaggregateTevent)this).SetTaggregateVersionInternal(taggregateVersion);
         ((IMutableTaggregateTevent)this).SetUtcTimeStampInternal(utcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
      }

      // ReSharper disable once MemberCanBePrivate.Local
      public string Test1 { [UsedImplicitly] get; private set; }

      // ReSharper disable once MemberCanBePrivate.Local
      public string Test2 { [UsedImplicitly] get; private set; }
   }
}
