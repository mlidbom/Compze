using Compze.Tessaging.Endpoints;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tests.Infrastructure;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;

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
            .RegisterComponents(registrar => registrar.RequirePerformanceTestTypeMappings())
            .RegisterTessageHandlers(handle => handle
                       .ForTuery((MyRemoteTuery _) => new MyTueryResult())
                       .ForTuery((MyLocalStrictlyLocalTuery _) => new MyTueryResult())));
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      Client = await TypermediaTestClient.ConnectTo(ServerEndpoint.Address!, registrar => registrar.RequirePerformanceTestTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await Client.DisposeAsync().caf();
      await Host.DisposeAsync().caf();
   }

   protected internal class MyRemoteTuery : Remotable.NonTransactional.Tueries.Tuery<MyTueryResult>;
   protected internal class MyLocalStrictlyLocalTuery : StrictlyLocal.Tueries.StrictlyLocalTuery<MyLocalStrictlyLocalTuery, MyTueryResult>;
   protected internal class MyTueryResult;
}
