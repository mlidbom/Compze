using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Serialization.Internal;
using Compze.Serialization.Newtonsoft.Private.TeventStore;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Compze.Tests.Performance.Internals.Serialization;

public class TeventStoreTeventSerializerPerformanceTests : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly ITeventStoreSerializer _teventSerializer;

   public TeventStoreTeventSerializerPerformanceTests()
   {
      _container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();
      _container.Register().TypeMapper();
      _teventSerializer = _container.ServiceLocator.Resolve<ITeventStoreSerializer>();
   }

   protected override void DisposeInternal() => _container.Dispose();

   [PCT] public void Should_roundtrip_simple_tevent_1000_times_in_25_milliseconds()
   {
      var tevent = new TestTevent(
         test1: "Test1",
         test2: "Test2",
         taggregateId: new TaggregateId(),
         taggregateVersion: 2,
         utcTimeStamp: DateTime.UtcNow + 1.Minutes());

      //Warmup
      _teventSerializer.Deserialize(typeof(TestTevent), _teventSerializer.Serialize(tevent));

      TimeAsserter.Execute(
         () =>
         {
            var teventJson = _teventSerializer.Serialize(tevent);
            _teventSerializer.Deserialize(typeof(TestTevent), teventJson);
         },
         iterations: 1000,
         maxTotal: 25.Milliseconds()
      );
   }

   [PCT] public void Should_roundtrip_simple_tevent_within_80_percent_of_default_serializer_performance()
   {
      const int iterations = 1000;
      const double allowedSlowdown = 1.8;

      var tevents = 1.Through(iterations).Select(_ => new TestTevent(
                                                    test1: "Test1",
                                                    test2: "Test2",
                                                    taggregateId: new TaggregateId(),
                                                    taggregateVersion: 2,
                                                    utcTimeStamp: DateTime.UtcNow + 1.Minutes())).ToList();

      var settings = NewtonsoftTeventStoreSerializer.JsonSettings;

      //Warmup
      _teventSerializer.Deserialize(typeof(TestTevent), _teventSerializer.Serialize(tevents.First()));
      JsonConvert.DeserializeObject<TestTevent>(JsonConvert.SerializeObject(tevents.First(), settings), settings);

      var defaultSerializerPerformanceNumbers = StopwatchCE.TimeExecution(() =>
      {
         var teventJson = tevents.Select(it => JsonConvert.SerializeObject(it, settings))
                                 .ToList();
         teventJson.ForEach(it => JsonConvert.DeserializeObject<TestTevent>(it, settings));
      });

      var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown).EnvMultiply(unoptimized: 1.2);

      TimeAsserter.Execute(() =>
                           {
                              var teventJson = tevents.Select(_teventSerializer.Serialize)
                                                      .ToList();
                              teventJson.ForEach(it => _teventSerializer.Deserialize(typeof(TestTevent), it));
                           },
                           maxTotal: allowedTime);
   }
}
