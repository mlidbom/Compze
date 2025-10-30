using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;

namespace Compze.Tests.Unit.Internals.Serialization;

public class NewtonSoftTeventStoreTeventSerializerTests : SerializerTest
{
   [PCTSerializer]
   public void IgnoresAllITaggregateTeventProperties()
   {
      var teventWithAllValuesSet = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         taggregateId:  new TaggregateId(),
         taggregateVersion:  2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      var teventWithOnlySubclassValues = new TestTevent("Test1", "Test2");
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)teventWithOnlySubclassValues).SetUtcTimeStampInternal(DateTime.MinValue);
#pragma warning restore CS0618 // Type or member is obsolete

      var teventWithAllValuesJson = TeventSerializer.Serialize(teventWithAllValuesSet);
      var teventWithOnlySubclassValuesJson = TeventSerializer.Serialize(teventWithOnlySubclassValues);
      var roundTripped = (TestTevent)TeventSerializer.Deserialize(typeof(TestTevent), teventWithAllValuesJson);

      teventWithAllValuesJson.Should().Be("""
                                         {
                                           "Test1": "Test1",
                                           "Test2": "Test2"
                                         }
                                         """);
      teventWithAllValuesJson.Should().Be(teventWithOnlySubclassValuesJson);

      roundTripped.Should().BeEquivalentTo(teventWithOnlySubclassValues,
                                           config => config
                                                    .PreferringRuntimeMemberTypes()
                                                    .ComparingByMembers<TaggregateTevent>()
                                                    .Excluding(tevent => tevent.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                                                    .Excluding(tevent => tevent.Id)
      );
   }

   public class TestTevent : TaggregateTevent
   {
      [JsonConstructor]public TestTevent(string test1, string test2)
      {
         Test1 = test1;
         Test2 = test2;
      }

      public TestTevent(
         string test1,
         string test2,
         int taggregateVersion,
         TaggregateId taggregateId,
         DateTime utcTimeStamp):base(taggregateId)
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