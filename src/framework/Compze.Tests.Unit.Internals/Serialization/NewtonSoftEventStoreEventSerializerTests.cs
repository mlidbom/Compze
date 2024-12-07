using System;
using Compze.Logging;
using Compze.Persistence.EventStore;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.SystemCE;
using Compze.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Compze.Tests.Unit.Internals.Serialization;

[TestFixture]
public class NewtonSoftEventStoreEventSerializerTests : UniversalTestBase
{
   IEventStoreSerializer _eventSerializer;

   [OneTimeSetUp] public void SetupTask() => _eventSerializer = new EventStoreSerializer(new TypeMapper());

   internal class TestEvent : AggregateEvent
   {
      [UsedImplicitly]
      public TestEvent() { }

      public TestEvent(string test1, string test2)
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

         ((IMutableAggregateEvent)this).SetAggregateVersion(aggregateVersion);
         ((IMutableAggregateEvent)this).SetUtcTimeStamp(utcTimeStamp);
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
      ((IMutableAggregateEvent)eventWithOnlySubclassValues).SetUtcTimeStamp(DateTime.MinValue);

      var eventWithAllValuesJson = _eventSerializer.Serialize(eventWithAllValuesSet);
      var eventWithOnlySubclassValuesJson = _eventSerializer.Serialize(eventWithOnlySubclassValues);
      var roundTripped = (TestEvent)_eventSerializer.Deserialize(typeof(TestEvent), eventWithAllValuesJson);

      ConsoleCE.WriteLine(eventWithAllValuesJson);

      eventWithAllValuesJson.Should().Be("""
                                         {
                                           "Test1": "Test1",
                                           "Test2": "Test2"
                                         }
                                         """);
      eventWithAllValuesJson.Should().Be(eventWithOnlySubclassValuesJson);

      roundTripped.Should().BeEquivalentTo(eventWithOnlySubclassValues,
                                           config => config
                                                    .RespectingRuntimeTypes()
                                                    .ComparingByMembers<AggregateEvent>()
                                                    .Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                                                    .Excluding(@event => @event.MessageId)
      );
   }
}