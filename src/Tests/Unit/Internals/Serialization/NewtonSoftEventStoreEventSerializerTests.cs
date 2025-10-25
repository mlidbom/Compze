using System;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Common.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Tests.Unit.Internals.Serialization;


public class NewtonSoftEventStoreEventSerializerTests : UniversalTestBase
{
   readonly IEventStoreSerializer _eventSerializer = new EventStoreSerializer(TypeMapper.Instance);

   public class TestTevent : AggregateTevent
   {
      [JsonConstructor]public TestTevent(string test1, string test2)
      {
         Test1 = test1;
         Test2 = test2;
      }

      public TestTevent(
         string test1,
         string test2,
         int aggregateVersion,
         Guid aggregateId,
         DateTime utcTimeStamp):base(aggregateId)
      {
         Test1 = test1;
         Test2 = test2;

#pragma warning disable CS0618 // Type or member is obsolete
         ((IMutableAggregateTevent)this).SetAggregateVersionInternal(aggregateVersion);
         ((IMutableAggregateTevent)this).SetUtcTimeStampInternal(utcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // ReSharper disable once MemberCanBePrivate.Local
        public string Test1 { [UsedImplicitly] get; private set; }
      // ReSharper disable once MemberCanBePrivate.Local
      public string Test2 { [UsedImplicitly] get; private set; }
   }


   [XF]
   public void IgnoresAllIAggregateEventProperties()
   {
      var eventWithAllValuesSet = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         aggregateId:  Guid.NewGuid(),
         aggregateVersion:  2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      var eventWithOnlySubclassValues = new TestTevent("Test1", "Test2");
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateTevent)eventWithOnlySubclassValues).SetUtcTimeStampInternal(DateTime.MinValue);
#pragma warning restore CS0618 // Type or member is obsolete

      var eventWithAllValuesJson = _eventSerializer.Serialize(eventWithAllValuesSet);
      var eventWithOnlySubclassValuesJson = _eventSerializer.Serialize(eventWithOnlySubclassValues);
      var roundTripped = (TestTevent)_eventSerializer.Deserialize(typeof(TestTevent), eventWithAllValuesJson);

      eventWithAllValuesJson.Should().Be("""
                                         {
                                           "Test1": "Test1",
                                           "Test2": "Test2"
                                         }
                                         """);
      eventWithAllValuesJson.Should().Be(eventWithOnlySubclassValuesJson);

      roundTripped.Should().BeEquivalentTo(eventWithOnlySubclassValues,
                                           config => config
                                                    .PreferringRuntimeMemberTypes()
                                                    .ComparingByMembers<AggregateTevent>()
                                                    .Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                                                    .Excluding(@event => @event.TessageId)
      );
   }
}