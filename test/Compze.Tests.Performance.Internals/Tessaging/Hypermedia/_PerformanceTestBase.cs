using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Typermedia;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public abstract class PerformanceTestBase : UniversalTestBase
{
   ITestingEndpointHost Host { get; set; }
   protected IEndpoint ServerEndpoint { get; set; }
   protected TestClient Client { get; private set; } = null!;
   protected IRemoteTypermediaNavigator Navigator => Client.Navigator;
   protected IInProcessTypermediaNavigator InProcessNavigator => ServerEndpoint.ServiceLocator.Resolve<IInProcessTypermediaNavigator>();

   protected PerformanceTestBase()
   {
      Host = TestingEndpointHost.Create();
      ServerEndpoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA7")),
         builder =>
         {
            builder.RegisterTypermediaHandlers
                   .ForTuery((MyRemoteTuery _) => new MyTueryResult())
                   .ForTuery((MyLocalStrictlyLocalTuery _) => new MyTueryResult());
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      Client = await TestClient.ConnectTo(ServerEndpoint.Address!).caf();
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
