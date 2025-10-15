using System;
using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Typermedia.Abstractions;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

//ncrunch: no coverage start

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public abstract class PerformanceTestBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected ITestingEndpointHost Host { get; set; }
   protected IEndpoint ServerEndpoint { get; set; }
   public IEndpoint ClientEndpoint { get; set; }
   protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
   protected IInProcessHypermediaNavigator InProcessNavigator => ServerEndpoint.ServiceLocator.Resolve<IInProcessHypermediaNavigator>();

   [SetUp] public async Task Setup()
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
                   .ForQuery((MyRemoteQuery _) => new MyQueryResult())
                   .ForQuery((MyLocalStrictlyLocalQuery _) => new MyQueryResult());
         });

      ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();
      await Host.StartAsync();
   }

   [TearDown] public async Task TearDown() => await Host.DisposeAsync();

   protected internal class MyRemoteQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult>;
   protected internal class MyLocalStrictlyLocalQuery : MessageTypes.StrictlyLocal.Queries.StrictlyLocalQuery<MyLocalStrictlyLocalQuery, MyQueryResult>;
   protected internal class MyQueryResult;
}