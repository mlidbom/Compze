using System;
using System.Linq;
using Compze.Common.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Newtonsoft.Json;

namespace Compze.Tests.Performance.Internals.Serialization;

public class NewtonSoftEventStoreEventSerializerPerformanceTests : UniversalTestBase
{
   static IEventStoreSerializer _eventSerializer = new EventStoreSerializer(TypeMapper.Instance);
   

   [XF] public void Should_roundtrip_simple_event_1000_times_in_15_milliseconds()
   {
      var @event = new TestEvent(
         test1: "Test1",
         test2: "Test2",
         aggregateId: Guid.NewGuid(),
         aggregateVersion: 2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      //Warmup
      _eventSerializer.Deserialize(typeof(TestEvent), _eventSerializer.Serialize(@event));

      TimeAsserter.Execute(
         () =>
         {
            var eventJson = _eventSerializer.Serialize(@event);
            _eventSerializer.Deserialize(typeof(TestEvent), eventJson);
         },
         iterations:1000,
         maxTotal: 15.Milliseconds()
      );
   }

   [XF] public void Should_roundtrip_simple_event_within_50_percent_of_default_serializer_performance()
   {
      const int iterations = 1000;
      const double allowedSlowdown = 1.5;

      var events = 1.Through(iterations).Select( _ =>  new TestEvent(
                                                    test1: "Test1",
                                                    test2: "Test2",
                                                    aggregateId: Guid.NewGuid(),
                                                    aggregateVersion: 2,
                                                    utcTimeStamp: DateTime.Now + 1.Minutes())).ToList();

      var settings = EventStoreSerializer.JsonSettings;

      //Warmup
      _eventSerializer.Deserialize(typeof(TestEvent), _eventSerializer.Serialize(events.First()));
      JsonConvert.DeserializeObject<TestEvent>(JsonConvert.SerializeObject(events.First(), settings), settings);

      var defaultSerializerPerformanceNumbers = StopwatchCE.TimeExecution(() =>
      {
         var eventJson = events.Select(it => JsonConvert.SerializeObject(it, settings))
                               .ToList();
         eventJson.ForEach(it => JsonConvert.DeserializeObject<TestEvent>(it, settings));
      });

      var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown).EnvMultiply(unoptimized:1.2);


      TimeAsserter.Execute(() =>
                           {
                              var eventJson = events.Select(_eventSerializer.Serialize)
                                                    .ToList();
                              eventJson.ForEach(it => _eventSerializer.Deserialize(typeof(TestEvent), it));
                           },
                           maxTotal: allowedTime);
   }
}