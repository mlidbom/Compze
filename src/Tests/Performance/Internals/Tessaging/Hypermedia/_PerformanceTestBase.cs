using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public abstract class PerformanceTestBase : UniversalTestBase
{
   protected ITestingEndpointHost Host { get; set; }
   protected IEndpoint ServerEndpoint { get; set; }
   public IEndpoint ClientEndpoint { get; set; }
   protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
   protected IInProcessHypermediaNavigator InProcessNavigator => ServerEndpoint.ServiceLocator.Resolve<IInProcessHypermediaNavigator>();

   protected PerformanceTestBase()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.CreateWithRegisteredServiceLocator);
      ServerEndpoint = Host.RegisterEndpoint(
         "Backend",
         new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA7")),
         builder =>
         {
            builder.Container.Register()
                   .AspNetCoreTransport()
                   .CurrentTestsConfiguredSqlLayer();
            builder.RegisterHandlers
                   .ForQuery((MyRemoteTuery _) => new MyQueryResult())
                   .ForQuery((MyLocalStrictlyLocalTuery _) => new MyQueryResult());
         });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
   }

   protected override async Task InitializeAsyncInternal() => await Host.StartAsync();

   protected override async Task DisposeAsyncInternal() => await Host.DisposeAsync();

   protected internal class MyRemoteTuery : TessageTypes.Remotable.NonTransactional.Queries.Tuery<MyQueryResult>;
   protected internal class MyLocalStrictlyLocalTuery : TessageTypes.StrictlyLocal.Queries.StrictlyLocalTuery<MyLocalStrictlyLocalTuery, MyQueryResult>;
   protected internal class MyQueryResult;
}
