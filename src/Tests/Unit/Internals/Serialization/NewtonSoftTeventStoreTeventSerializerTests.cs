using System;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Common.Refactoring.Naming;
using Compze.Serialization;
using Compze.Serialization.Newtonsoft;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Tests.Unit.Internals.Serialization;


public class NewtonSoftTeventStoreTeventSerializerTests : UniversalTestBase
{
   readonly ITeventStoreSerializer _teventSerializer = new TeventStoreSerializer(TypeMapper.Instance);

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
         Guid taggregateId,
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


   [XF]
   public void IgnoresAllITaggregateTeventProperties()
   {
      var teventWithAllValuesSet = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         taggregateId:  Guid.NewGuid(),
         taggregateVersion:  2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      var teventWithOnlySubclassValues = new TestTevent("Test1", "Test2");
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)teventWithOnlySubclassValues).SetUtcTimeStampInternal(DateTime.MinValue);
#pragma warning restore CS0618 // Type or member is obsolete

      var teventWithAllValuesJson = _teventSerializer.Serialize(teventWithAllValuesSet);
      var teventWithOnlySubclassValuesJson = _teventSerializer.Serialize(teventWithOnlySubclassValues);
      var roundTripped = (TestTevent)_teventSerializer.Deserialize(typeof(TestTevent), teventWithAllValuesJson);

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
                                                    .Excluding(@tevent => @tevent.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                                                    .Excluding(@tevent => @tevent.TessageId)
      );
   }
}