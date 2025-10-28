using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public abstract class PerformanceTestBase : UniversalTestBase
{
   protected ITestingEndpointHost Host { get; set; }
   protected IEndpoint ServerEndpoint { get; set; }
   public IEndpoint ClientEndpoint { get; set; }
   protected IRemoteTypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>();
   protected IInProcessTypermediaNavigator InProcessNavigator => ServerEndpoint.ServiceLocator.Resolve<IInProcessTypermediaNavigator>();

   protected PerformanceTestBase()
   {
      Host = TestingEndpointHost.Create();
      ServerEndpoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA7")),
         builder =>
         {
            builder.RegisterHandlers
                   .ForTuery((MyRemoteTuery _) => new MyTueryResult())
                   .ForTuery((MyLocalStrictlyLocalTuery _) => new MyTueryResult());
         });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected override async Task InitializeAsyncInternal() => await Host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await Host.DisposeAsync();

   protected internal class MyRemoteTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<MyTueryResult>;
   protected internal class MyLocalStrictlyLocalTuery : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<MyLocalStrictlyLocalTuery, MyTueryResult>;
   protected internal class MyTueryResult;
}
