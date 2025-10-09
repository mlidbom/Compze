using System;
using Compze.Common.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Unit.Internals.Serialization;

[TestFixture]
public class NewtonSoftEventStoreEventSerializerTests : UniversalTestBase
{
   IEventStoreSerializer _eventSerializer;

   [OneTimeSetUp] public void SetupTask() => _eventSerializer = new EventStoreSerializer(TypeMapper.Instance);

   public class TestEvent : AggregateEvent
   {
      [JsonConstructor]public TestEvent(string test1, string test2)
      {
         Test1 = test1;
         Test2 = test2;
      }

      public TestEvent(
         string test1,
         string test2,
         int aggregateVersion,
         Guid aggregateId,
         DateTime utcTimeStamp):base(aggregateId)
      {
         Test1 = test1;
         Test2 = test2;

#pragma warning disable CS0618 // Type or member is obsolete
         ((IMutableAggregateEvent)this).SetAggregateVersionInternal(aggregateVersion);
         ((IMutableAggregateEvent)this).SetUtcTimeStampInternal(utcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // ReSharper disable once MemberCanBePrivate.Local
        public string Test1 { [UsedImplicitly] get; private set; }
      // ReSharper disable once MemberCanBePrivate.Local
      public string Test2 { [UsedImplicitly] get; private set; }
   }


   [Test]
   public void IgnoresAllIAggregateEventProperties()
   {
      var eventWithAllValuesSet = new TestEvent(
         test1: "Test1",
         test2: "Test2",
         aggregateId:  Guid.NewGuid(),
         aggregateVersion:  2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      var eventWithOnlySubclassValues = new TestEvent("Test1", "Test2");
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateEvent)eventWithOnlySubclassValues).SetUtcTimeStampInternal(DateTime.MinValue);
#pragma warning restore CS0618 // Type or member is obsolete

      var eventWithAllValuesJson = _eventSerializer.Serialize(eventWithAllValuesSet);
      var eventWithOnlySubclassValuesJson = _eventSerializer.Serialize(eventWithOnlySubclassValues);
      var roundTripped = (TestEvent)_eventSerializer.Deserialize(typeof(TestEvent), eventWithAllValuesJson);

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
                                                    .ComparingByMembers<AggregateEvent>()
                                                    .Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                                                    .Excluding(@event => @event.MessageId)
      );
   }
}