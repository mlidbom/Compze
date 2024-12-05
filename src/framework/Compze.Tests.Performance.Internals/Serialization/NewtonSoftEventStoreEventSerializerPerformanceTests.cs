using System;
using System.Linq;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using Compze.Testing.Performance;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Compze.Tests.Serialization;

public class NewtonSoftEventStoreEventSerializerPerformanceTests : UniversalTestBase
{
   IEventStoreSerializer _eventSerializer;

   [OneTimeSetUp] public void SetupTask() => _eventSerializer = new EventStoreSerializer(new TypeMapper());

   [Test] public void Should_roundtrip_simple_event_1000_times_in_15_milliseconds()
   {
      var @event = new NewtonSoftEventStoreEventSerializerTests.TestEvent(
         test1: "Test1",
         test2: "Test2",
         aggregateId: Guid.NewGuid(),
         aggregateVersion: 2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      //Warmup
      _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), _eventSerializer.Serialize(@event));

      TimeAsserter.Execute(
         () =>
         {
            var eventJson = _eventSerializer.Serialize(@event);
            _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), eventJson);
         },
         iterations:1000,
         maxTotal: 15.Milliseconds()
      );
   }

   [Test] public void Should_roundtrip_simple_event_within_50_percent_of_default_serializer_performance()
   {
      const int iterations = 1000;
      const double allowedSlowdown = 1.5;

      var events = 1.Through(iterations).Select( _ =>  new NewtonSoftEventStoreEventSerializerTests.TestEvent(
                                                    test1: "Test1",
                                                    test2: "Test2",
                                                    aggregateId: Guid.NewGuid(),
                                                    aggregateVersion: 2,
                                                    utcTimeStamp: DateTime.Now + 1.Minutes())).ToList();

      var settings = EventStoreSerializer.JsonSettings;

      //Warmup
      _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), _eventSerializer.Serialize(events.First()));
      JsonConvert.DeserializeObject<NewtonSoftEventStoreEventSerializerTests.TestEvent>(JsonConvert.SerializeObject(events.First(), settings), settings);

      var defaultSerializerPerformanceNumbers = StopwatchCE.TimeExecution(() =>
      {
         var eventJson = events.Select(it => JsonConvert.SerializeObject(it, settings))
                               .ToList();
         eventJson.ForEach(it => JsonConvert.DeserializeObject<NewtonSoftEventStoreEventSerializerTests.TestEvent>(it, settings));
      });

      var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown).EnvMultiply(unoptimized:1.2);


      TimeAsserter.Execute(() =>
                           {
                              var eventJson = events.Select(_eventSerializer.Serialize)
                                                    .ToList();
                              eventJson.ForEach(it => _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), it));
                           },
                           maxTotal: allowedTime);
   }
}