using System;
using System.Linq;
using Compze.Abstractions.Serialization.Internal;
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

public class NewtonSoftTeventStoreTeventSerializerPerformanceTests : UniversalTestBase
{
   static ITeventStoreSerializer _teventSerializer = new TeventStoreSerializer(TypeMapper.Instance);
   

   [XF] public void Should_roundtrip_simple_tevent_1000_times_in_25_milliseconds()
   {
      var @tevent = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         aggregateId: Guid.NewGuid(),
         aggregateVersion: 2,
         utcTimeStamp: DateTime.Now + 1.Minutes());

      //Warmup
      _teventSerializer.Deserialize(typeof(TestTevent), _teventSerializer.Serialize(@tevent));

      TimeAsserter.Execute(
         () =>
         {
            var teventJson = _teventSerializer.Serialize(@tevent);
            _teventSerializer.Deserialize(typeof(TestTevent), teventJson);
         },
         iterations:1000,
         maxTotal: 25.Milliseconds()
      );
   }

   [XF] public void Should_roundtrip_simple_tevent_within_50_percent_of_default_serializer_performance()
   {
      const int iterations = 1000;
      const double allowedSlowdown = 1.5;

      var tevents = 1.Through(iterations).Select( _ =>  new TestTevent(
                                                    test1: "Test1",
                                                    test2: "Test2",
                                                    aggregateId: Guid.NewGuid(),
                                                    aggregateVersion: 2,
                                                    utcTimeStamp: DateTime.Now + 1.Minutes())).ToList();

      var settings = TeventStoreSerializer.JsonSettings;

      //Warmup
      _teventSerializer.Deserialize(typeof(TestTevent), _teventSerializer.Serialize(tevents.First()));
      JsonConvert.DeserializeObject<TestTevent>(JsonConvert.SerializeObject(tevents.First(), settings), settings);

      var defaultSerializerPerformanceNumbers = StopwatchCE.TimeExecution(() =>
      {
         var teventJson = tevents.Select(it => JsonConvert.SerializeObject(it, settings))
                               .ToList();
         teventJson.ForEach(it => JsonConvert.DeserializeObject<TestTevent>(it, settings));
      });

      var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown).EnvMultiply(unoptimized:1.2);


      TimeAsserter.Execute(() =>
                           {
                              var teventJson = tevents.Select(_teventSerializer.Serialize)
                                                    .ToList();
                              teventJson.ForEach(it => _teventSerializer.Deserialize(typeof(TestTevent), it));
                           },
                           maxTotal: allowedTime);
   }
}