using Compze.Abstractions.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Runtime;
using Compze.Internals.Serialization.Newtonsoft.Private.TeventStore;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.TeventStore.Abstractions.Internal;
using Newtonsoft.Json;

namespace Compze.Tests.Performance.Internals.Serialization;

public class TeventStoreTeventSerializerPerformanceTests : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly ITeventStoreSerializer _teventSerializer;

   public TeventStoreTeventSerializerPerformanceTests()
   {
      var builder = TestEnv.DIContainer.CreateWithCurrentTestsPluggableComponents();
      builder.Registrar.TypeIdentifierMapper(mapper => mapper.RegisterPerformanceTestTypeMappings());
      _container = builder.Build();
      _teventSerializer = _container.Resolve<ITeventStoreSerializer>();
   }

   protected override void DisposeInternal() => _container.Dispose();

   [PCT] public void Should_roundtrip_simple_tevent_1000_times_in_50_milliseconds()
   {
      var wrappedTevent = new TaggregateTevent<TestTevent>(new TestTevent(
         test1: "Test1",
         test2: "Test2",
         taggregateId: new TaggregateId(),
         taggregateVersion: 2,
         utcTimeStamp: DateTime.UtcNow + 1.Minutes()));

      //Warmup
      _teventSerializer.Deserialize(typeof(TaggregateTevent<TestTevent>), _teventSerializer.Serialize(wrappedTevent));

      TimeAsserter.Execute(
         () =>
         {
            var teventJson = _teventSerializer.Serialize(wrappedTevent);
            _teventSerializer.Deserialize(typeof(TaggregateTevent<TestTevent>), teventJson);
         },
         iterations: 1000,
         maxTotal: 50.Milliseconds()
      );
   }

   [PCT] public void Should_roundtrip_simple_tevent_within_80_percent_of_default_serializer_performance()
   {
      const int iterations = 1000;
      const double allowedSlowdown = 1.8;

      var wrappedTevents = 1.Through(iterations).Select(_ => new TaggregateTevent<TestTevent>(new TestTevent(
                                                    test1: "Test1",
                                                    test2: "Test2",
                                                    taggregateId: new TaggregateId(),
                                                    taggregateVersion: 2,
                                                    utcTimeStamp: DateTime.UtcNow + 1.Minutes()))).ToList();

      var settings = NewtonsoftTeventStoreSerializer.JsonSettings;

      //Warmup
      _teventSerializer.Deserialize(typeof(TaggregateTevent<TestTevent>), _teventSerializer.Serialize(wrappedTevents.First()));
      JsonConvert.DeserializeObject<TaggregateTevent<TestTevent>>(JsonConvert.SerializeObject(wrappedTevents.First(), settings), settings);

      var defaultSerializerPerformanceNumbers = StopwatchCE.TimeExecution(() =>
      {
         var teventJson = wrappedTevents.Select(it => JsonConvert.SerializeObject(it, settings))
                                 .ToList();
         teventJson.ForEach(it => JsonConvert.DeserializeObject<TaggregateTevent<TestTevent>>(it, settings));
      });

      var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown).EnvMultiply(unoptimized: 1.2);

      TimeAsserter.Execute(() =>
                           {
                              var teventJson = wrappedTevents.Select(_teventSerializer.Serialize)
                                                      .ToList();
                              teventJson.ForEach(it => _teventSerializer.Deserialize(typeof(TaggregateTevent<TestTevent>), it));
                           },
                           maxTotal: allowedTime);
   }
}
