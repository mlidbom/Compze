using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Typermedia.Client;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Engine;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public abstract class PerformanceTestBase : UniversalTestBase
{
   TestingEndpointHost Host { get; }
   protected BestEffortEndpoint ServerEndpoint { get; }
   TypermediaTestClient Client { get; set; } = null!;
   protected IRemoteTypermediaNavigator Navigator => Client.Navigator;

   protected PerformanceTestBase()
   {
      Host = TestingEndpointHost.Create();
      ServerEndpoint = Host.RegisterBestEffortEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA7")),
         endpointBuilder => endpointBuilder
            .MapTypes(mapper => mapper.RegisterPerformanceTestTypeMappings())
            .RegisterTessageHandlers(handle => handle
                       .ForTuery((MyRemoteTuery _) => new MyTueryResult())
                       .ForTuery((MyLocalStrictlyLocalTuery _) => new MyTueryResult())));
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      Client = await TypermediaTestClient.ConnectTo(ServerEndpoint.Address!, mapper => mapper.RegisterPerformanceTestTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await Client.DisposeAsync().caf();
      await Host.DisposeAsync().caf();
   }

   protected internal class MyRemoteTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<MyTueryResult>;
   protected internal class MyLocalStrictlyLocalTuery : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<MyLocalStrictlyLocalTuery, MyTueryResult>;
   protected internal class MyTueryResult;
}
